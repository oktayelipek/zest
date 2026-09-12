using Zest.Domain.Customers;
using Zest.Domain.Economy;
using Zest.Domain.Inventory;
using Zest.Domain.Operations;
using Zest.Domain.Products;
using Zest.Domain.World;

namespace Zest.Domain.DayCycle;

/// <summary>
/// Executes the authoritative live loop: world opportunity, customer choice,
/// reservation, operational work, service, inventory consumption and ledger.
/// It deliberately has no presentation dependency, so Godot and headless runs
/// advance the same state with the same commands.
/// </summary>
public sealed class LiveDayRunner
{
    private readonly GameState _state;
    private readonly RecipeBook _recipes;
    private readonly CustomerGenerationProfile _customers;
    private readonly WorldGenerationProfile _worldProfile;
    private readonly DateOnly _firstDate;
    private readonly WorldGenerator _world = new();
    private readonly CustomerGenerator _customerGenerator = new();
    private readonly OrderScheduler _scheduler;
    private readonly Dictionary<Guid, RecipeVersion> _orderRecipes = [];
    private WorldDayContext? _day;
    private int _generatedThroughHour = 7;
    private long _processedSimTime;
    private const int OperationalStepSeconds = 15;

    public LiveDayRunner(GameState state, RecipeBook recipes, CustomerGenerationProfile customers,
        WorldGenerationProfile worldProfile, DateOnly firstDate)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _worldProfile = worldProfile ?? throw new ArgumentNullException(nameof(worldProfile));
        _firstDate = firstDate;
        _scheduler = new OrderScheduler(state.Operations);
    }

    public void RegisterResource(ResourceKind kind, string id, int capacity = 1) =>
        _scheduler.RegisterResource(kind, id, capacity);

    public string? WeatherId => _day?.WeatherId;
    public decimal? TemperatureC => _day?.TemperatureC;

    public PreparedBatchState PrepareExtraBatch(string productId, int servings, int freshnessSeconds) =>
        new LiveInterventionService(_state).PrepareExtraBatch(RecipeFor(productId), servings,
            _state.Progression.Has(UpgradeIds.BiggerCooler) ? checked(freshnessSeconds * 2) : freshnessSeconds);

    public int SellableServings(string productId)
    {
        RecipeVersion recipe = RecipeFor(productId);
        InventoryService inventory = new(_state.Inventory);
        int raw = recipe.Components.Select(component => decimal.ToInt32(decimal.Floor(inventory.Available(component.IngredientId) / component.Quantity))).Min();
        int prepared = _state.Operations.Interventions.PreparedBatches.Where(batch => batch.ProductId == productId && batch.ExpiresAtSimTime > _state.SimTime).Sum(batch => batch.RemainingServings);
        return productId == "classic" ? Math.Min(_state.DayCycle.RemainingOpeningServings, raw) + prepared : raw + prepared;
    }

    public long CurrentPrice(string productId) => _state.Operations.Interventions.CurrentPrices.GetValueOrDefault(productId, RecipeFor(productId).SalePriceMinor);

    public LivePriceChange ChangePrice(string productId, long priceMinor) =>
        new LiveInterventionService(_state).ChangeLivePrice(productId, priceMinor, basePriceMinor: RecipeFor(productId).SalePriceMinor);

    /// <summary>Restore the runner's internal position for a mid-day quicksave reload.
    /// Regenerates the deterministic world day, marks past hours as already-generated so they are not replayed,
    /// and starts with an empty order book (any active queue is intentionally dropped on load).</summary>
    public void ResumeMidDay()
    {
        DateOnly date = _firstDate.AddDays(_state.DayIndex);
        _day = _world.GenerateDayContext(_state, date, _worldProfile);
        // Skip regenerating any hour whose start is at or before the saved SimTime;
        // partial current-hour traffic is intentionally lost on resume.
        int hoursElapsed = Math.Clamp((int)(_state.SimTime / 3600), 0, 10);
        _generatedThroughHour = Math.Max(7, 8 + hoursElapsed);
        _processedSimTime = _state.SimTime;
        _orderRecipes.Clear();
    }

    public void StartDay()
    {
        if (_state.Business.Ledger.Count == 0 && _state.Business.OpeningCashMinorUnits == 0)
            new EconomyService(_state.Business).InitializeOpeningCash(new Money(10_000));
        DateOnly date = _firstDate.AddDays(_state.DayIndex);
        _day = _world.GenerateDayContext(_state, date, _worldProfile);
        _generatedThroughHour = 7;
        _processedSimTime = _state.SimTime;
        _orderRecipes.Clear();
        GenerateHoursThrough(8);
    }

    public void AdvanceTo(long targetSimTime)
    {
        if (targetSimTime < _processedSimTime) throw new ArgumentOutOfRangeException(nameof(targetSimTime));
        if (_day is null) throw new InvalidOperationException("Live day runner has not been started.");
        while (_processedSimTime + OperationalStepSeconds <= targetSimTime)
        {
            long cursor = _processedSimTime + OperationalStepSeconds;
            _state.SimTime = cursor;
            GenerateHoursThrough(Math.Min(17, 8 + (int)(cursor / 3600)));
            new LiveInterventionService(_state).AdvanceTo(cursor);
            _scheduler.AdvanceTo(cursor);
            EvaluateAbandonment(cursor);
            ServeReadyOrders(cursor);
            _processedSimTime = cursor;
        }
        _state.SimTime = targetSimTime;
    }

    /// <summary>Closing does not leak active work or inventory holds into the next day.</summary>
    public void CloseDay()
    {
        InventoryService inventory = new(_state.Inventory);
        foreach (Guid id in _state.Operations.ActiveOrderIds.ToArray())
        {
            if (!_scheduler.CancelOrder(id, _state.SimTime, OrderCancellationReason.ClosingTime)) continue;
            inventory.Release(id.ToString("N"));
            OrderState order = _state.Operations.Orders[id];
            _state.Operations.RecordLoss(new(id, order.CustomerId, Customers.LostSaleReason.ClosingTime,
                _state.DayIndex, _state.SimTime, _state.SimTime - order.AcceptedAtSimTime, 1));
        }
    }

    private void GenerateHoursThrough(int hour)
    {
        if (_day is null) return;
        DateOnly date = _day.Time.Date;
        for (int current = Math.Max(8, _generatedThroughHour + 1); current <= hour; current++)
        {
            foreach (PasserbyOpportunity opportunity in _world.GenerateHour(
                         _state, new CalendarTimeState(date, current), _day, _worldProfile))
                TryAccept(opportunity, (current - 8) * 3600L);
            _generatedThroughHour = current;
        }
    }

    private void TryAccept(PasserbyOpportunity opportunity, long acceptedAt)
    {
        CustomerGenerationProfile priced = FilterToActiveMenu(ApplyReputation(new LiveInterventionService(_state).ApplyCurrentPrices(_customers)));
        CustomerInteraction interaction = _customerGenerator.Generate(_state, opportunity, priced);
        if (interaction.Decision != CustomerDecision.Purchased || interaction.RecipeId is null) return;
        // A delivered/prepared batch extends Classic beyond the opening plan; raw stock alone does not.
        if (interaction.RecipeId == "classic" && SellableServings("classic") <= 0) return;
        RecipeVersion source = RecipeFor(interaction.RecipeId);
        long price = _state.Operations.Interventions.CurrentPrices.GetValueOrDefault(source.Id.RecipeId, source.SalePriceMinor);
        RecipeVersion recipe = source with
        {
            SalePriceMinor = price,
            WorkSteps = _state.Progression.Has(UpgradeIds.BetterCounter) || _state.Progression.Has(UpgradeIds.ElectricJuicer)
                ? source.WorkSteps.Select(step => step with
                {
                    DurationSeconds = Math.Max(1, step.DurationSeconds
                        - (_state.Progression.Has(UpgradeIds.BetterCounter) ? 10 : 0)
                        - (_state.Progression.Has(UpgradeIds.ElectricJuicer) ? 10 : 0))
                }).ToArray()
                : source.WorkSteps,
        };
        Guid orderId = interaction.Customer.Id;
        PreparedBatchState? prepared = _state.Operations.Interventions.PreparedBatches
            .Where(batch => batch.ProductId == recipe.Id.RecipeId && batch.RemainingServings > 0 && batch.ExpiresAtSimTime > acceptedAt)
            .OrderBy(batch => batch.ExpiresAtSimTime).FirstOrDefault();
        InventoryService inventory = new(_state.Inventory);
        if (prepared is null && !inventory.TryReserve(orderId.ToString("N"), recipe)) return;
        if (!new LiveInterventionService(_state).TryAcceptOrder(_scheduler, orderId, recipe, acceptedAt,
                out OrderState? order, interaction.Customer.Id.ToString("N")) || order is null)
        {
            if (prepared is null) inventory.Release(orderId.ToString("N"));
            return;
        }
        if (interaction.RecipeId == "classic") _state.DayCycle.RemainingOpeningServings--;
        _orderRecipes.Add(orderId, prepared is { PrepaidCostMinor: > 0 } ? recipe with { CogsMinor = 0 } : recipe);
    }

    private void EvaluateAbandonment(long simTime)
    {
        QueueAbandonmentService abandonment = new(_state, _scheduler);
        foreach (Guid id in _state.Operations.ActiveOrderIds.ToArray())
        {
            AbandonmentResult result = abandonment.Evaluate(id, simTime);
            if (result.Outcome == AbandonmentOutcome.Abandoned)
                if (_orderRecipes.TryGetValue(id, out RecipeVersion? recipe) && recipe.Id.RecipeId == "classic") _state.DayCycle.RemainingOpeningServings++;
        }
    }

    private void ServeReadyOrders(long simTime)
    {
        InventoryService inventory = new(_state.Inventory);
        EconomyService economy = new(_state.Business);
        foreach (OrderState order in _state.Operations.Orders.Values
                     .Where(order => order.Status == OrderStatus.Ready).OrderBy(order => order.Sequence).ToArray())
        {
            if (!_scheduler.ServeOrder(order.OrderId, simTime)) continue;
            string reservationId = order.OrderId.ToString("N");
            if (inventory.CanRelease(reservationId) && !inventory.Consume(reservationId))
                throw new InvalidOperationException($"Unable to consume inventory for '{order.OrderId}'.");
            if (!economy.PostServedOrder(reservationId, _orderRecipes[order.OrderId], simTime, _state.DayIndex))
                throw new InvalidOperationException($"Duplicate financial service for '{order.OrderId}'.");
        }
    }

    private RecipeVersion RecipeFor(string productId) => _recipes.Versions.Values
        .Single(recipe => recipe.Id.RecipeId == productId);

    private CustomerGenerationProfile FilterToActiveMenu(CustomerGenerationProfile profile)
    {
        // Classic is always on-menu. Optional recipes require an explicit unlock decision.
        Dictionary<string, ProductChoiceProfile> allowed = profile.Products
            .Where(pair => pair.Key switch
            {
                "classic" => true,
                "berry" => _state.Progression.Has(MenuIds.Berry),
                "strong" => _state.Progression.Has(MenuIds.Strong),
                _ => false,
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return allowed.Count == profile.Products.Count ? profile : profile with { Products = allowed };
    }

    private CustomerGenerationProfile ApplyReputation(CustomerGenerationProfile profile)
    {
        decimal factor = .8m + _state.Progression.Reputation / 250m;
        Dictionary<string, CustomerSegmentProfile> segments = profile.Segments.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with { InterestProbability = decimal.Clamp(pair.Value.InterestProbability * factor, 0, 1) },
            StringComparer.Ordinal);
        return profile with { Segments = segments };
    }
}

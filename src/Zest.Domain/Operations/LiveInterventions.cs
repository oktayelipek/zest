using System.Collections.ObjectModel;
using Zest.Domain.DayCycle;
using Zest.Domain.Customers;
using Zest.Domain.Economy;
using Zest.Domain.Products;
using Zest.Domain.Inventory;

namespace Zest.Domain.Operations;

public enum LiveInterventionKind { RushMenu, PrepareExtraBatch, DisableProduct, EmergencyRestock, CallExtraHelp, MiddayPriceChange }
public enum InterventionTraceStage { Trigger, Action, Consequence }

public sealed record InterventionTraceEvent(
    Guid InterventionId,
    LiveInterventionKind Kind,
    InterventionTraceStage Stage,
    long SimTime,
    string Detail);

public sealed record ProductCoverageLoss(string ProductId, long SimTime, string Reason);
public sealed record EmergencyRestockState(Guid Id, string ProductId, int Servings, int DayIndex, long RequestedAtSimTime, long ArrivesAtSimTime, long ChargedCostMinor, int FreshnessSeconds)
{
    public bool Arrived { get; internal set; }
}
public sealed record ExtraHelpState(Guid Id, string StaffId, long CalledAtSimTime, long ArrivesAtSimTime, long LeavesAtSimTime, long LaborCostMinor)
{
    public bool CapacityApplied { get; internal set; }
    public bool ShiftEnded { get; internal set; }
}
public sealed record LivePriceChange(Guid Id, string ProductId, long PreviousPriceMinor, long NewPriceMinor, long SimTime);

public sealed class PreparedBatchState
{
    internal PreparedBatchState(Guid batchId, string productId, int servings, long preparedAt, long expiresAt, long prepaidCostMinor = 0, long wasteCostMinor = 0)
    {
        BatchId = batchId;
        ProductId = productId;
        InitialServings = servings;
        RemainingServings = servings;
        PreparedAtSimTime = preparedAt;
        ExpiresAtSimTime = expiresAt;
        PrepaidCostMinor = prepaidCostMinor;
        WasteCostMinor = wasteCostMinor;
    }

    public Guid BatchId { get; }
    public string ProductId { get; }
    public int InitialServings { get; }
    public int RemainingServings { get; internal set; }
    public int WastedServings { get; internal set; }
    public long PreparedAtSimTime { get; }
    public long ExpiresAtSimTime { get; }
    /// <summary>Cost already charged at delivery; it must not become cash COGS again on service.</summary>
    public long PrepaidCostMinor { get; }
    public long WasteCostMinor { get; }
}

public sealed class LiveInterventionState
{
    private readonly HashSet<string> _rushMenuProductIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _disabledUntil = new(StringComparer.Ordinal);
    private readonly List<PreparedBatchState> _preparedBatches = [];
    private readonly List<ProductCoverageLoss> _coverageLosses = [];
    private readonly List<InterventionTraceEvent> _trace = [];
    private readonly Dictionary<LiveInterventionKind, long> _cooldownUntil = [];
    private readonly Dictionary<LiveInterventionKind, Guid> _activeInterventionIds = [];
    private readonly List<EmergencyRestockState> _emergencyRestocks = [];
    private readonly List<ExtraHelpState> _extraHelpCalls = [];
    private readonly List<LivePriceChange> _priceChanges = [];
    private readonly Dictionary<string, long> _currentPrices = new(StringComparer.Ordinal);

    public IReadOnlySet<string> RushMenuProductIds => _rushMenuProductIds.ToHashSet(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, long> DisabledUntil => new ReadOnlyDictionary<string, long>(_disabledUntil);
    public IReadOnlyList<PreparedBatchState> PreparedBatches => _preparedBatches.AsReadOnly();
    public IReadOnlyList<ProductCoverageLoss> CoverageLosses => _coverageLosses.AsReadOnly();
    public IReadOnlyList<InterventionTraceEvent> Trace => _trace.AsReadOnly();
    public IReadOnlyDictionary<LiveInterventionKind, long> CooldownUntil => new ReadOnlyDictionary<LiveInterventionKind, long>(_cooldownUntil);
    public long FrictionUntilSimTime { get; internal set; }
    public IReadOnlyList<EmergencyRestockState> EmergencyRestocks => _emergencyRestocks.AsReadOnly();
    public IReadOnlyList<ExtraHelpState> ExtraHelpCalls => _extraHelpCalls.AsReadOnly();
    public IReadOnlyList<LivePriceChange> PriceChanges => _priceChanges.AsReadOnly();
    public IReadOnlyDictionary<string, long> CurrentPrices => new ReadOnlyDictionary<string, long>(_currentPrices);
    internal long NextInterventionSequence { get; set; }

    internal void SetRushMenu(IEnumerable<string> productIds)
    {
        _rushMenuProductIds.Clear();
        foreach (string productId in productIds) _rushMenuProductIds.Add(productId);
    }

    internal void Disable(string productId, long until) => _disabledUntil[productId] = until;
    internal bool IsDisabled(string productId, long simTime) => _disabledUntil.TryGetValue(productId, out long until) && simTime < until;
    internal void AddBatch(PreparedBatchState batch) => _preparedBatches.Add(batch);
    internal void AddCoverageLoss(ProductCoverageLoss loss) => _coverageLosses.Add(loss);
    internal void AddTrace(InterventionTraceEvent item) => _trace.Add(item);
    internal void SetCooldown(LiveInterventionKind kind, long until) => _cooldownUntil[kind] = until;
    internal void SetActiveId(LiveInterventionKind kind, Guid id) => _activeInterventionIds[kind] = id;
    internal Guid ActiveId(LiveInterventionKind kind) => _activeInterventionIds.GetValueOrDefault(kind);
    internal void AddRestock(EmergencyRestockState item) => _emergencyRestocks.Add(item);
    internal void AddExtraHelp(ExtraHelpState item) => _extraHelpCalls.Add(item);
    internal void AddPriceChange(LivePriceChange item) { _priceChanges.Add(item); _currentPrices[item.ProductId] = item.NewPriceMinor; }

    internal void RestoreForQuicksave(
        IEnumerable<PreparedBatchState>? preparedBatches,
        IReadOnlyDictionary<string, long>? currentPrices,
        IReadOnlyDictionary<string, long>? disabledUntil,
        IEnumerable<string>? rushMenu)
    {
        _preparedBatches.Clear();
        if (preparedBatches is not null) _preparedBatches.AddRange(preparedBatches);
        _currentPrices.Clear();
        if (currentPrices is not null)
            foreach (var pair in currentPrices) _currentPrices[pair.Key] = pair.Value;
        _disabledUntil.Clear();
        if (disabledUntil is not null)
            foreach (var pair in disabledUntil) _disabledUntil[pair.Key] = pair.Value;
        _rushMenuProductIds.Clear();
        if (rushMenu is not null) foreach (var id in rushMenu) _rushMenuProductIds.Add(id);
    }

    /// <summary>Test/factory hook to reconstruct a prepared batch outside the intervention service.</summary>
    public static PreparedBatchState HydrateBatch(Guid id, string productId, int initial, int remaining, long preparedAt, long expiresAt, long prepaidCost) =>
        new(id, productId, initial, preparedAt, expiresAt, prepaidCost) { RemainingServings = remaining };
}

/// <summary>Authoritative command boundary for live-day operational interventions.</summary>
public sealed class LiveInterventionService
{
    public const int DefaultCooldownSeconds = 300;
    public const int DefaultFrictionSeconds = 15;
    public const int MaxEmergencyRestocksPerDay = 2;

    private readonly GameState _gameState;
    private readonly LiveInterventionState _state;

    public LiveInterventionService(GameState gameState)
    {
        _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        _state = gameState.Operations.Interventions;
    }

    public void ActivateRushMenu(IEnumerable<string> productIds) =>
        ActivateRushMenu(productIds, DefaultCooldownSeconds, DefaultFrictionSeconds);

    public void ActivateRushMenu(IEnumerable<string> productIds, int cooldownSeconds, int frictionSeconds)
    {
        string[] menu = productIds?.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray()
            ?? throw new ArgumentNullException(nameof(productIds));
        if (menu.Length == 0 || menu.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Rush menu needs at least one valid product.", nameof(productIds));
        Guid id = Begin(LiveInterventionKind.RushMenu, cooldownSeconds, frictionSeconds, $"Queue pressure triggered a {menu.Length}-product rush menu.");
        _state.SetRushMenu(menu);
        Action(id, LiveInterventionKind.RushMenu, $"Rush menu set to: {string.Join(", ", menu)}.");
        Consequence(id, LiveInterventionKind.RushMenu, "Future demand outside the rush menu loses coverage; task durations and capacities are unchanged.");
    }

    public PreparedBatchState PrepareExtraBatch(string productId, int servings, int freshnessSeconds) =>
        PrepareExtraBatch(productId, servings, freshnessSeconds, DefaultCooldownSeconds, DefaultFrictionSeconds);

    public PreparedBatchState PrepareExtraBatch(string productId, int servings, int freshnessSeconds, int cooldownSeconds, int frictionSeconds)
        => PrepareExtraBatchCore(productId, servings, freshnessSeconds, cooldownSeconds, frictionSeconds, null);

    public PreparedBatchState PrepareExtraBatch(RecipeVersion recipe, int servings, int freshnessSeconds, int cooldownSeconds = DefaultCooldownSeconds, int frictionSeconds = DefaultFrictionSeconds)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        return PrepareExtraBatchCore(recipe.Id.RecipeId, servings, freshnessSeconds, cooldownSeconds, frictionSeconds, recipe);
    }

    private PreparedBatchState PrepareExtraBatchCore(string productId, int servings, int freshnessSeconds, int cooldownSeconds, int frictionSeconds, RecipeVersion? recipe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (servings <= 0) throw new ArgumentOutOfRangeException(nameof(servings));
        if (freshnessSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(freshnessSeconds));
        Guid id = Begin(LiveInterventionKind.PrepareExtraBatch, cooldownSeconds, frictionSeconds, $"Workload pressure triggered extra prep for {productId}.");
        if (recipe is not null)
        {
            InventoryService inventory = new(_gameState.Inventory);
            string reservationId = $"prepared-batch:{id:N}";
            if (!inventory.TryReserve(reservationId, recipe, servings) || !inventory.Consume(reservationId))
                throw new InvalidOperationException($"Insufficient raw inventory to prepare {servings} servings of {productId}.");
        }
        PreparedBatchState batch = new(id, productId, servings, _gameState.SimTime, checked(_gameState.SimTime + freshnessSeconds), wasteCostMinor: checked((recipe?.CogsMinor ?? 0) * servings));
        _state.AddBatch(batch);
        Action(id, LiveInterventionKind.PrepareExtraBatch, $"Prepared {servings} servings of {productId} in advance.");
        Consequence(id, LiveInterventionKind.PrepareExtraBatch, "The first recipe work step is prepaid per serving; unused servings become waste after freshness expires.");
        return batch;
    }

    public void TemporarilyDisableProduct(string productId) =>
        TemporarilyDisableProduct(productId, 900, DefaultCooldownSeconds, DefaultFrictionSeconds);

    public void TemporarilyDisableProduct(string productId, int durationSeconds, int cooldownSeconds, int frictionSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        Guid id = Begin(LiveInterventionKind.DisableProduct, cooldownSeconds, frictionSeconds, $"Operational pressure triggered disabling {productId}.");
        long until = checked(_gameState.SimTime + durationSeconds);
        _state.Disable(productId, until);
        Action(id, LiveInterventionKind.DisableProduct, $"Disabled {productId} until simulation time {until}.");
        Consequence(id, LiveInterventionKind.DisableProduct, $"Menu coverage for {productId} is zero during the intervention; existing orders are unchanged.");
    }

    public EmergencyRestockState RequestEmergencyRestock(string productId, int servings, int arrivalDelaySeconds, long normalUnitCostMinor,
        decimal premiumMultiplier = 1.5m, int freshnessSeconds = 1800, int cooldownSeconds = DefaultCooldownSeconds, int frictionSeconds = DefaultFrictionSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (servings <= 0 || arrivalDelaySeconds <= 0 || normalUnitCostMinor <= 0 || freshnessSeconds <= 0) throw new ArgumentOutOfRangeException();
        if (premiumMultiplier <= 1m) throw new ArgumentOutOfRangeException(nameof(premiumMultiplier), "Emergency supply must cost more than normal supply.");
        if (_state.EmergencyRestocks.Count(item => item.DayIndex == _gameState.DayIndex) >= MaxEmergencyRestocksPerDay)
            throw new InvalidOperationException("Emergency restock daily limit reached.");
        Guid id = Begin(LiveInterventionKind.EmergencyRestock, cooldownSeconds, frictionSeconds, $"Low stock triggered emergency supply for {productId}.");
        long cost = checked((long)decimal.Ceiling(servings * normalUnitCostMinor * premiumMultiplier));
        new EconomyService(_gameState.Business).PostExpense(LedgerEntryType.Supply, new Money(cost), $"emergency-restock:{id:N}", _gameState.SimTime, _gameState.DayIndex);
        EmergencyRestockState item = new(id, productId, servings, _gameState.DayIndex, _gameState.SimTime, checked(_gameState.SimTime + arrivalDelaySeconds), cost, freshnessSeconds);
        _state.AddRestock(item);
        Action(id, LiveInterventionKind.EmergencyRestock, $"Ordered {servings} emergency servings at {premiumMultiplier:0.##}× normal cost; ETA {arrivalDelaySeconds}s.");
        Consequence(id, LiveInterventionKind.EmergencyRestock, "Cash is charged now; stock remains unavailable until delivery.");
        return item;
    }

    public ExtraHelpState CallExtraHelp(string staffId, int arrivalDelaySeconds, int durationSeconds, long laborCostMinor,
        int cooldownSeconds = DefaultCooldownSeconds, int frictionSeconds = DefaultFrictionSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(staffId);
        if (arrivalDelaySeconds <= 0 || durationSeconds <= 0 || laborCostMinor <= 0) throw new ArgumentOutOfRangeException();
        ResourceKey key = new(ResourceKind.Staff, staffId);
        if (!_gameState.Operations.Resources.ContainsKey(key)) throw new KeyNotFoundException($"Unknown staff resource '{staffId}'.");
        Guid id = Begin(LiveInterventionKind.CallExtraHelp, cooldownSeconds, frictionSeconds, "Queue pressure triggered a call for extra help.");
        new EconomyService(_gameState.Business).PostExpense(LedgerEntryType.Labor, new Money(laborCostMinor), $"extra-help:{id:N}", _gameState.SimTime, _gameState.DayIndex);
        long arrival = checked(_gameState.SimTime + arrivalDelaySeconds);
        ExtraHelpState item = new(id, staffId, _gameState.SimTime, arrival, checked(arrival + durationSeconds), laborCostMinor);
        _state.AddExtraHelp(item);
        Action(id, LiveInterventionKind.CallExtraHelp, $"Extra help called; ETA {arrivalDelaySeconds}s for a {durationSeconds}s shift.");
        Consequence(id, LiveInterventionKind.CallExtraHelp, "Labor cost is committed immediately; capacity is unchanged until arrival.");
        return item;
    }

    public LivePriceChange ChangeLivePrice(string productId, long newPriceMinor, int cooldownSeconds = DefaultCooldownSeconds, int frictionSeconds = DefaultFrictionSeconds, long? basePriceMinor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (newPriceMinor <= 0) throw new ArgumentOutOfRangeException(nameof(newPriceMinor));
        long previous = _state.CurrentPrices.GetValueOrDefault(productId, basePriceMinor ?? _gameState.DayCycle.PlannedPriceMinor);
        if (previous == newPriceMinor) throw new InvalidOperationException("New live price must differ from the current price.");
        Guid id = Begin(LiveInterventionKind.MiddayPriceChange, cooldownSeconds, frictionSeconds, $"Margin or demand pressure triggered repricing {productId}.");
        LivePriceChange item = new(id, productId, previous, newPriceMinor, _gameState.SimTime);
        _state.AddPriceChange(item);
        if (productId == "classic") _gameState.DayCycle.PlannedPriceMinor = newPriceMinor;
        Action(id, LiveInterventionKind.MiddayPriceChange, $"Changed {productId} price from {previous} to {newPriceMinor} minor units.");
        Consequence(id, LiveInterventionKind.MiddayPriceChange, "Future choices use the normal price-sensitivity model; segment reference prices remain unchanged.");
        return item;
    }

    public CustomerGenerationProfile ApplyCurrentPrices(CustomerGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Dictionary<string, ProductChoiceProfile> products = profile.Products.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with { PriceMinor = _state.CurrentPrices.GetValueOrDefault(pair.Key, pair.Value.PriceMinor) },
            StringComparer.Ordinal);
        return profile with { Products = products };
    }

    public bool TryAcceptOrder(OrderScheduler scheduler, Guid orderId, RecipeVersion recipe, long acceptedAtSimTime, out OrderState? order, string? customerId = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(recipe);
        AdvanceTo(acceptedAtSimTime);
        string productId = recipe.Id.RecipeId;
        string? rejection = _state.IsDisabled(productId, acceptedAtSimTime)
            ? "product disabled"
            : _state.RushMenuProductIds.Count > 0 && !_state.RushMenuProductIds.Contains(productId)
                ? "outside rush menu"
                : null;
        if (rejection is not null)
        {
            _state.AddCoverageLoss(new(productId, acceptedAtSimTime, rejection));
            LiveInterventionKind kind = rejection == "product disabled" ? LiveInterventionKind.DisableProduct : LiveInterventionKind.RushMenu;
            Consequence(_state.ActiveId(kind), kind, $"Rejected demand for {productId}: {rejection}.", acceptedAtSimTime);
            order = null;
            return false;
        }

        PreparedBatchState? batch = _state.PreparedBatches
            .Where(item => item.ProductId == productId && item.RemainingServings > 0 && item.ExpiresAtSimTime > acceptedAtSimTime)
            .OrderBy(item => item.ExpiresAtSimTime)
            .ThenBy(item => item.PreparedAtSimTime)
            .FirstOrDefault();
        IReadOnlyList<RecipeWorkStep> plan = recipe.WorkSteps;
        if (batch is not null)
        {
            batch.RemainingServings--;
            plan = recipe.WorkSteps.Skip(1).ToArray();
            Consequence(batch.BatchId, LiveInterventionKind.PrepareExtraBatch,
                $"Batch serving used for {productId}; skipped explicit first work step.", acceptedAtSimTime);
        }
        order = scheduler.AcceptOrderWithWorkPlan(orderId, recipe, plan, acceptedAtSimTime, customerId);
        return true;
    }

    public void AdvanceTo(long simTime)
    {
        if (simTime < _gameState.SimTime) throw new ArgumentOutOfRangeException(nameof(simTime));
        foreach (EmergencyRestockState restock in _state.EmergencyRestocks.Where(item => !item.Arrived && item.ArrivesAtSimTime <= simTime))
        {
            restock.Arrived = true;
            _state.AddBatch(new PreparedBatchState(restock.Id, restock.ProductId, restock.Servings, restock.ArrivesAtSimTime, checked(restock.ArrivesAtSimTime + restock.FreshnessSeconds), restock.ChargedCostMinor, restock.ChargedCostMinor));
            Consequence(restock.Id, LiveInterventionKind.EmergencyRestock, $"Emergency delivery arrived with {restock.Servings} servings of {restock.ProductId}.", restock.ArrivesAtSimTime);
        }
        foreach (ExtraHelpState help in _state.ExtraHelpCalls.Where(item => !item.CapacityApplied && item.ArrivesAtSimTime <= simTime))
        {
            help.CapacityApplied = true;
            _gameState.Operations.Resources[new ResourceKey(ResourceKind.Staff, help.StaffId)].Capacity++;
            Consequence(help.Id, LiveInterventionKind.CallExtraHelp, "Extra help arrived; staff capacity increased by one.", help.ArrivesAtSimTime);
        }
        foreach (ExtraHelpState help in _state.ExtraHelpCalls.Where(item => item.CapacityApplied && !item.ShiftEnded && item.LeavesAtSimTime <= simTime))
        {
            help.ShiftEnded = true;
            _gameState.Operations.Resources[new ResourceKey(ResourceKind.Staff, help.StaffId)].Capacity--;
            Consequence(help.Id, LiveInterventionKind.CallExtraHelp, "Extra-help shift ended; temporary capacity removed.", help.LeavesAtSimTime);
        }
        foreach (PreparedBatchState batch in _state.PreparedBatches.Where(item => item.RemainingServings > 0 && item.ExpiresAtSimTime <= simTime))
        {
            batch.WastedServings += batch.RemainingServings;
            if (batch.WasteCostMinor > 0)
            {
                long wasteCost = checked(batch.WasteCostMinor * batch.RemainingServings / batch.InitialServings);
                new EconomyService(_gameState.Business).PostExpense(LedgerEntryType.Waste, new Money(wasteCost), $"expired-batch:{batch.BatchId:N}", simTime, _gameState.DayIndex, affectsCash: false);
            }
            batch.RemainingServings = 0;
            Consequence(batch.BatchId, LiveInterventionKind.PrepareExtraBatch,
                $"{batch.WastedServings} unused servings of {batch.ProductId} expired as waste.", simTime);
        }
    }

    private Guid Begin(LiveInterventionKind kind, int cooldownSeconds, int frictionSeconds, string trigger)
    {
        if (_gameState.DayCycle.Phase != DayPhase.Live) throw new InvalidOperationException("Interventions are available only during the live phase.");
        if (cooldownSeconds < 0) throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
        if (frictionSeconds < 0) throw new ArgumentOutOfRangeException(nameof(frictionSeconds));
        long now = _gameState.SimTime;
        if (now < _state.FrictionUntilSimTime) throw new InvalidOperationException($"Intervention friction lasts until { _state.FrictionUntilSimTime }.");
        if (_state.CooldownUntil.TryGetValue(kind, out long until) && now < until) throw new InvalidOperationException($"{kind} cooldown lasts until {until}.");
        long sequence = ++_state.NextInterventionSequence;
        Guid id = new(_gameState.DayIndex, (short)kind, 0, BitConverter.GetBytes(sequence));
        _state.FrictionUntilSimTime = checked(now + frictionSeconds);
        _state.SetCooldown(kind, checked(now + cooldownSeconds));
        _state.SetActiveId(kind, id);
        _state.AddTrace(new(id, kind, InterventionTraceStage.Trigger, now, trigger));
        return id;
    }

    private void Action(Guid id, LiveInterventionKind kind, string detail) =>
        _state.AddTrace(new(id, kind, InterventionTraceStage.Action, _gameState.SimTime, detail));

    private void Consequence(Guid id, LiveInterventionKind kind, string detail, long? simTime = null) =>
        _state.AddTrace(new(id, kind, InterventionTraceStage.Consequence, simTime ?? _gameState.SimTime, detail));
}

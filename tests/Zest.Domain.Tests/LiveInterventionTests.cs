using Zest.Domain;
using Zest.Domain.DayCycle;
using Zest.Domain.Operations;
using Zest.Domain.Products;
using Zest.Domain.Customers;
using Zest.Domain.Economy;
using Zest.Domain.World;
using Zest.Telemetry;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class LiveInterventionTests
{
    [Fact]
    public void RushMenuChangesCoverageButNeverTaskSpeedOrCapacity()
    {
        GameState state = LiveState();
        OrderScheduler scheduler = Scheduler(state);
        LiveInterventionService service = new(state);
        RecipeVersion classic = Recipe("classic", 20, 10);
        RecipeVersion berry = Recipe("berry", 20, 10);
        int capacityBefore = state.Operations.Resources.Values.Single(item => item.Key.Kind == ResourceKind.Staff).Capacity;

        service.ActivateRushMenu(["classic"], cooldownSeconds: 60, frictionSeconds: 0);

        Assert.True(service.TryAcceptOrder(scheduler, Id(1), classic, 0, out OrderState? accepted));
        Assert.False(service.TryAcceptOrder(scheduler, Id(2), berry, 0, out OrderState? rejected));
        Assert.Null(rejected);
        Assert.Equal([20, 10], accepted!.TaskIds.Select(id => state.Operations.Tasks[id].Step.DurationSeconds));
        Assert.Equal(capacityBefore, state.Operations.Resources.Values.Single(item => item.Key.Kind == ResourceKind.Staff).Capacity);
        Assert.Single(state.Operations.Interventions.CoverageLosses);
    }

    [Fact]
    public void ExtraBatchPrepaysRealWorkAndExpiresIntoWaste()
    {
        GameState state = LiveState();
        OrderScheduler scheduler = Scheduler(state);
        LiveInterventionService service = new(state);
        RecipeVersion recipe = Recipe("classic", 20, 10);
        PreparedBatchState batch = service.PrepareExtraBatch("classic", 2, freshnessSeconds: 30, cooldownSeconds: 60, frictionSeconds: 0);

        Assert.True(service.TryAcceptOrder(scheduler, Id(1), recipe, 0, out OrderState? order));
        Assert.Single(order!.TaskIds);
        Assert.Equal(10, state.Operations.Tasks[order.TaskIds[0]].Step.DurationSeconds);
        Assert.Equal(1, batch.RemainingServings);

        service.AdvanceTo(30);
        Assert.Equal(0, batch.RemainingServings);
        Assert.Equal(1, batch.WastedServings);
    }

    [Fact]
    public void DisabledProductLosesCoverageButKeepsExistingOrder()
    {
        GameState state = LiveState();
        OrderScheduler scheduler = Scheduler(state);
        LiveInterventionService service = new(state);
        RecipeVersion recipe = Recipe("classic", 20, 10);
        OrderState existing = scheduler.AcceptOrder(Id(1), recipe, 0);

        service.TemporarilyDisableProduct("classic", durationSeconds: 120, cooldownSeconds: 60, frictionSeconds: 0);

        Assert.False(service.TryAcceptOrder(scheduler, Id(2), recipe, 0, out _));
        Assert.NotEqual(OrderStatus.Cancelled, existing.Status);
        Assert.Equal("classic", Assert.Single(state.Operations.Interventions.CoverageLosses).ProductId);
        Assert.True(service.TryAcceptOrder(scheduler, Id(3), recipe, 120, out _));
    }

    [Fact]
    public void CooldownAndGlobalFrictionBlockRepeatedInterventions()
    {
        GameState state = LiveState();
        LiveInterventionService service = new(state);
        service.ActivateRushMenu(["classic"], cooldownSeconds: 60, frictionSeconds: 15);

        Assert.Throws<InvalidOperationException>(() => service.PrepareExtraBatch("classic", 2, 30, 60, 15));
        state.SimTime = 15;
        service.PrepareExtraBatch("classic", 2, 30, 60, 0);
        Assert.Throws<InvalidOperationException>(() => service.ActivateRushMenu(["classic"], 60, 0));
        state.SimTime = 60;
        service.ActivateRushMenu(["classic"], 60, 0);
    }

    [Fact]
    public void EveryInterventionEmitsTriggerActionAndConsequenceTelemetry()
    {
        GameState state = LiveState();
        LiveInterventionService service = new(state);
        service.ActivateRushMenu(["classic"], 0, 0);
        service.PrepareExtraBatch("classic", 2, 30, 0, 0);
        service.TemporarilyDisableProduct("berry", 120, 0, 0);

        foreach (IGrouping<Guid, InterventionTraceEvent> trace in state.Operations.Interventions.Trace.GroupBy(item => item.InterventionId))
        {
            Assert.Contains(trace, item => item.Stage == InterventionTraceStage.Trigger);
            Assert.Contains(trace, item => item.Stage == InterventionTraceStage.Action);
            Assert.Contains(trace, item => item.Stage == InterventionTraceStage.Consequence);
        }
        InterventionTraceSummary summary = InterventionTraceReporter.Build(state.Operations);
        Assert.Equal(3, summary.InterventionCount);
        Assert.Equal(3, summary.TriggerCount);
        Assert.Equal(3, summary.ActionCount);
        Assert.Equal(3, summary.ConsequenceCount);
    }

    [Fact]
    public void EmergencyRestockChargesPremiumAndArrivesAfterDelayWithDailyLimit()
    {
        GameState state = LiveState();
        LiveInterventionService service = new(state);

        EmergencyRestockState first = service.RequestEmergencyRestock("classic", 8, 120, 50, 1.75m, 600, 0, 0);
        Assert.Equal(700, first.ChargedCostMinor);
        Assert.Empty(state.Operations.Interventions.PreparedBatches);
        Assert.Equal(-700, state.Business.CashMinorUnits);
        service.AdvanceTo(119);
        Assert.Empty(state.Operations.Interventions.PreparedBatches);
        service.AdvanceTo(120);
        Assert.Equal(8, Assert.Single(state.Operations.Interventions.PreparedBatches).RemainingServings);

        service.RequestEmergencyRestock("classic", 2, 60, 50, 1.5m, 600, 0, 0);
        Assert.Throws<InvalidOperationException>(() => service.RequestEmergencyRestock("classic", 2, 60, 50, 1.5m, 600, 0, 0));
        Assert.Equal(850, new EconomyService(state.Business).ProjectDay(0).Supply.MinorUnits);
    }

    [Fact]
    public void ExtraHelpAddsCapacityOnlyAfterArrivalAndPostsLaborCost()
    {
        GameState state = LiveState();
        _ = Scheduler(state);
        LiveInterventionService service = new(state);
        ResourceKey owner = new(ResourceKind.Staff, "owner");

        service.CallExtraHelp("owner", 90, 60, 1200, 0, 0);
        Assert.Equal(1, state.Operations.Resources[owner].Capacity);
        Assert.Equal(-1200, state.Business.CashMinorUnits);
        service.AdvanceTo(89);
        Assert.Equal(1, state.Operations.Resources[owner].Capacity);
        service.AdvanceTo(90);
        Assert.Equal(2, state.Operations.Resources[owner].Capacity);
        service.AdvanceTo(150);
        Assert.Equal(1, state.Operations.Resources[owner].Capacity);
    }

    [Fact]
    public void MiddayPriceChangeUsesExistingCustomerPriceSensitivity()
    {
        GameState state = LiveState();
        LiveInterventionService service = new(state);
        CustomerGenerationProfile baseline = new(
            new Dictionary<string, CustomerSegmentProfile> { ["student"] = new("student", 350, 1, 1, 1, .25m, new Dictionary<string, decimal> { ["classic"] = 1 }) },
            new Dictionary<string, ProductChoiceProfile> { ["classic"] = new("classic", 350) });
        service.ChangeLivePrice("classic", 700, 0, 0);
        CustomerGenerationProfile repriced = service.ApplyCurrentPrices(baseline);
        Assert.Equal(350, baseline.Segments["student"].ReferencePriceMinor);
        Assert.Equal(700, repriced.Products["classic"].PriceMinor);

        PasserbyOpportunity opportunity = new("price-test", new CalendarTimeState(new DateOnly(2026, 9, 8), 12), "park", "student", "sunny", 24);
        CustomerGenerator generator = new();
        int before = 0, after = 0;
        for (ulong seed = 0; seed < 500; seed++)
        {
            if (generator.Generate(GameStateFactory.CreateEmpty(seed), opportunity, baseline).Decision == CustomerDecision.Purchased) before++;
            if (generator.Generate(GameStateFactory.CreateEmpty(seed), opportunity, repriced).Decision == CustomerDecision.Purchased) after++;
        }
        Assert.True(before > after);
    }

    private static GameState LiveState()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        DayCommandProcessor day = new(state);
        day.Execute(new StartLiveCommand());
        return state;
    }

    private static OrderScheduler Scheduler(GameState state)
    {
        OrderScheduler scheduler = new(state.Operations);
        scheduler.RegisterResource(ResourceKind.Station, "counter");
        scheduler.RegisterResource(ResourceKind.Equipment, "stand");
        scheduler.RegisterResource(ResourceKind.Staff, "owner");
        return scheduler;
    }

    private static RecipeVersion Recipe(string id, params int[] durations) => new(
        new RecipeVersionId(id, 1), [new RecipeComponent("lemon", 1, "each")], 350,
        new ProductProfile(.5m, .5m, .5m, .5m), 100, durations.Sum())
    {
        WorkSteps = durations.Select((duration, index) => new RecipeWorkStep($"step-{index}", duration, "counter", "stand", "owner")).ToArray(),
    };

    private static Guid Id(int value) => new(value, 0, 0, new byte[8]);
}

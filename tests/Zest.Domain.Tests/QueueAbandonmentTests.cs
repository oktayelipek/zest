using Zest.Domain.DayCycle;
using Zest.Domain.Inventory;
using Zest.Domain.Operations;
using Zest.Domain.Products;
using Zest.Telemetry;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class QueueAbandonmentTests
{
    [Fact]
    public void Abandonment_probability_increases_smoothly_with_wait()
    {
        QueuePatienceProfile profile = new(120, 2m, .6m, .98m);
        decimal[] probabilities = new long[] { 0, 30, 60, 120, 240 }
            .Select(wait => AbandonmentModel.Probability(wait, 120, profile))
            .ToArray();

        Assert.Equal(0, probabilities[0]);
        Assert.True(probabilities.Zip(probabilities.Skip(1)).All(pair => pair.First < pair.Second));
        Assert.All(probabilities, probability => Assert.InRange(probability, 0, profile.MaximumProbability));
        Assert.True(AbandonmentModel.Probability(180, 30, profile) > AbandonmentModel.Probability(180, 300, profile));
    }

    [Fact]
    public void Abandoned_order_releases_inventory_once_and_leaves_future_workload()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        OrderScheduler scheduler = CreateScheduler(state);
        InventoryService inventory = new(state.Inventory);
        RecipeVersion recipe = Recipe();
        inventory.ReceiveLot("lemons", "lemon", 2);
        Guid abandonedId = Id(1);
        Guid nextId = Id(2);
        Assert.True(inventory.TryReserve(abandonedId.ToString("N"), recipe));
        Assert.True(inventory.TryReserve(nextId.ToString("N"), recipe));
        OrderState abandoned = scheduler.AcceptOrder(abandonedId, recipe, 0, "customer-a", new QueuePatienceProfile(30, 2, .5m, 1));
        OrderState next = scheduler.AcceptOrder(nextId, recipe, 0, "customer-b");

        QueueAbandonmentService service = new(state, scheduler);
        AbandonmentResult result = service.Evaluate(abandonedId, 120);
        AbandonmentResult repeat = service.Evaluate(abandonedId, 121);

        Assert.Equal(AbandonmentOutcome.Abandoned, result.Outcome);
        Assert.True(result.ReservationReleased);
        Assert.Equal(AbandonmentOutcome.NotEligible, repeat.Outcome);
        Assert.Equal(OrderStatus.Cancelled, abandoned.Status);
        Assert.All(abandoned.TaskIds.Select(id => state.Operations.Tasks[id]), task => Assert.Equal(WorkTaskStatus.Cancelled, task.Status));
        Assert.Equal(OrderStatus.InProgress, next.Status);
        Assert.Equal(120, state.Operations.Tasks[next.TaskIds[0]].StartedAtSimTime);
        Assert.Equal(ReservationStatus.Released, state.Inventory.Reservations[abandonedId.ToString("N")].Status);
        Assert.False(inventory.Release(abandonedId.ToString("N")));
    }

    [Fact]
    public void Queue_loss_is_distinct_in_daily_and_telemetry_reports()
    {
        GameState state = GameStateFactory.CreateEmpty(9);
        DayCommandProcessor day = new(state);
        day.Execute(new StartLiveCommand());
        OrderScheduler scheduler = CreateScheduler(state);
        InventoryService inventory = new(state.Inventory);
        RecipeVersion recipe = Recipe();
        inventory.ReceiveLot("lemons", "lemon", 1);
        Guid orderId = Id(9);
        Assert.True(inventory.TryReserve(orderId.ToString("N"), recipe));
        scheduler.AcceptOrder(orderId, recipe, 0, "customer-9", new QueuePatienceProfile(10, 2, 1, 1));
        Assert.Equal(AbandonmentOutcome.Abandoned, new QueueAbandonmentService(state, scheduler).Evaluate(orderId, 120).Outcome);

        DaySessionSnapshot snapshot = day.Execute(new CloseDayCommand());
        QueueLossSummary telemetry = QueueLossReporter.Build(state.Operations, 0);

        Assert.Equal(1, snapshot.QueueLossCount);
        Assert.Equal(1, snapshot.Report!.QueueLossCount);
        Assert.Equal(1, telemetry.Count);
        Assert.Equal(120, telemetry.AverageWaitSeconds);
    }

    private static OrderScheduler CreateScheduler(GameState state)
    {
        OrderScheduler scheduler = new(state.Operations);
        scheduler.RegisterResource(ResourceKind.Station, "counter");
        scheduler.RegisterResource(ResourceKind.Equipment, "stand");
        scheduler.RegisterResource(ResourceKind.Staff, "owner");
        return scheduler;
    }

    private static RecipeVersion Recipe() => new(
        new RecipeVersionId("classic", 1), [new RecipeComponent("lemon", 1, "each")], 350,
        new ProductProfile(.5m, .5m, .5m, .5m), 60, 600)
    {
        WorkSteps =
        [
            new RecipeWorkStep("prepare", 300, "counter", "stand", "owner"),
            new RecipeWorkStep("finish", 300, "counter", "stand", "owner"),
        ],
    };

    private static Guid Id(int value) => new(value, 0, 0, new byte[8]);
}

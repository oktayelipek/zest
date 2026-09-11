using Zest.Domain.Operations;
using Zest.Domain.Products;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class OrderSchedulerTests
{
    [Fact]
    public void Load_below_capacity_stabilizes_the_queue()
    {
        OperationsState state = new();
        OrderScheduler scheduler = CreateScheduler(state);
        for (int order = 0; order < 20; order++) scheduler.AcceptOrder(Id(order), Recipe(10), order * 15L);
        scheduler.AdvanceTo(300);

        Assert.DoesNotContain(state.Tasks.Values, task => task.Status is WorkTaskStatus.Queued or WorkTaskStatus.Running);
        Assert.All(state.Orders.Values, order => Assert.Equal(OrderStatus.Ready, order.Status));
    }

    [Fact]
    public void Load_above_capacity_grows_the_fifo_queue()
    {
        OperationsState state = new();
        OrderScheduler scheduler = CreateScheduler(state);
        for (int order = 0; order < 20; order++) scheduler.AcceptOrder(Id(order), Recipe(10), order * 5L);

        WorkTaskState[] waiting = state.Tasks.Values.Where(task => task.Status == WorkTaskStatus.Queued).ToArray();
        Assert.True(waiting.Length >= 9);
        Assert.Equal(waiting.OrderBy(task => task.Sequence).Select(task => task.TaskId), waiting.OrderBy(task => task.EnqueuedAtSimTime).ThenBy(task => task.Sequence).Select(task => task.TaskId));
    }

    [Fact]
    public void Resources_are_never_double_booked()
    {
        OperationsState state = new();
        OrderScheduler scheduler = CreateScheduler(state);
        for (int order = 0; order < 8; order++) scheduler.AcceptOrder(Id(order), Recipe(10), 0);
        for (int time = 0; time <= 80; time++)
        {
            scheduler.AdvanceTo(time);
            Assert.All(state.Resources.Values, resource => Assert.True(resource.ActiveTaskIds.Count <= resource.Capacity));
        }
    }

    [Fact]
    public void Order_cannot_be_served_before_every_required_step_completes()
    {
        OperationsState state = new();
        OrderScheduler scheduler = CreateScheduler(state);
        RecipeVersion recipe = Recipe(5,
            new RecipeWorkStep("mix", 5, "counter", "stand", "owner"),
            new RecipeWorkStep("finish", 7, "counter", "stand", "owner"));
        Guid orderId = Id(42);

        OrderState order = scheduler.AcceptOrder(orderId, recipe, 0);
        Assert.False(scheduler.ServeOrder(orderId, 5));
        Assert.Equal(OrderStatus.InProgress, order.Status);
        Assert.False(scheduler.ServeOrder(orderId, 11));
        Assert.True(scheduler.ServeOrder(orderId, 12));
        Assert.Equal(12, order.ActualWaitSeconds);
    }

    [Fact]
    public void Expected_and_actual_wait_are_recorded()
    {
        OperationsState state = new();
        OrderScheduler scheduler = CreateScheduler(state);
        OrderState first = scheduler.AcceptOrder(Id(1), Recipe(10), 0);
        OrderState second = scheduler.AcceptOrder(Id(2), Recipe(10), 0);
        scheduler.AdvanceTo(20);

        Assert.Equal(10, first.ExpectedWaitSeconds);
        Assert.Equal(20, second.ExpectedWaitSeconds);
        Assert.Equal(10, first.ActualWaitSeconds);
        Assert.Equal(20, second.ActualWaitSeconds);
    }

    private static OrderScheduler CreateScheduler(OperationsState state)
    {
        OrderScheduler scheduler = new(state);
        scheduler.RegisterResource(ResourceKind.Station, "counter");
        scheduler.RegisterResource(ResourceKind.Equipment, "stand");
        scheduler.RegisterResource(ResourceKind.Staff, "owner");
        return scheduler;
    }

    private static RecipeVersion Recipe(int duration, params RecipeWorkStep[] steps) => new(
        new RecipeVersionId("classic", 1), [new RecipeComponent("lemon", 1, "each")], 350,
        new ProductProfile(.5m, .5m, .5m, .5m), 60, duration)
    {
        WorkSteps = steps.Length == 0 ? [new RecipeWorkStep("prepare", duration, "counter", "stand", "owner")] : Array.AsReadOnly(steps),
    };

    private static Guid Id(int value) => new(value + 1, 0, 0, new byte[8]);
}

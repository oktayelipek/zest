using Zest.Domain.Products;
using Zest.Domain.Customers;

namespace Zest.Domain.Operations;

public enum OrderStatus { Accepted, Queued, InProgress, Ready, Served, Cancelled }
public enum WorkTaskStatus { Blocked, Queued, Running, Completed, Cancelled }
public enum ResourceKind { Station, Equipment, Staff }
public enum OrderCancellationReason { CustomerAbandoned, ClosingTime }

public sealed record QueuePatienceProfile(
    int PatienceSeconds = 180,
    decimal Shape = 2m,
    decimal UnexpectedDelayWeight = .6m,
    decimal MaximumProbability = .98m);

public sealed record OrderLossEvent(
    Guid OrderId,
    string? CustomerId,
    LostSaleReason Reason,
    int DayIndex,
    long SimTime,
    long WaitSeconds,
    decimal Probability);

public readonly record struct ResourceKey(ResourceKind Kind, string Id)
{
    public override string ToString() => $"{Kind}:{Id}";
}

public sealed class OperationalResourceState
{
    private readonly HashSet<string> _activeTaskIds = new(StringComparer.Ordinal);

    internal OperationalResourceState(ResourceKey key, int capacity)
    {
        Key = key;
        Capacity = capacity;
    }

    public ResourceKey Key { get; }
    public int Capacity { get; internal set; }
    public IReadOnlySet<string> ActiveTaskIds => _activeTaskIds.ToHashSet(StringComparer.Ordinal);
    internal bool HasCapacity => _activeTaskIds.Count < Capacity;
    internal void Acquire(string taskId) => _activeTaskIds.Add(taskId);
    internal void Release(string taskId) => _activeTaskIds.Remove(taskId);
}

public sealed class WorkTaskState
{
    internal WorkTaskState(string taskId, Guid orderId, int stepIndex, RecipeWorkStep step, long sequence)
    {
        TaskId = taskId;
        OrderId = orderId;
        StepIndex = stepIndex;
        Step = step;
        Sequence = sequence;
    }

    public string TaskId { get; }
    public Guid OrderId { get; }
    public int StepIndex { get; }
    public RecipeWorkStep Step { get; }
    public long Sequence { get; }
    public WorkTaskStatus Status { get; internal set; } = WorkTaskStatus.Blocked;
    public long? EnqueuedAtSimTime { get; internal set; }
    public long? StartedAtSimTime { get; internal set; }
    public long? CompletedAtSimTime { get; internal set; }
    public IReadOnlyList<ResourceKey> RequiredResources =>
    [
        new(ResourceKind.Station, Step.StationId),
        new(ResourceKind.Equipment, Step.EquipmentId),
        new(ResourceKind.Staff, Step.StaffId),
    ];
}

public sealed class OrderState
{
    internal OrderState(Guid orderId, RecipeVersionId recipeVersionId, string? customerId, long acceptedAt, long sequence, IReadOnlyList<string> taskIds, long expectedWaitSeconds, QueuePatienceProfile patience)
    {
        OrderId = orderId;
        RecipeVersionId = recipeVersionId;
        CustomerId = customerId;
        AcceptedAtSimTime = acceptedAt;
        Sequence = sequence;
        TaskIds = Array.AsReadOnly(taskIds.ToArray());
        ExpectedWaitSeconds = expectedWaitSeconds;
        Patience = patience;
    }

    public Guid OrderId { get; }
    public RecipeVersionId RecipeVersionId { get; }
    public string? CustomerId { get; }
    public long AcceptedAtSimTime { get; }
    public long Sequence { get; }
    public IReadOnlyList<string> TaskIds { get; }
    public OrderStatus Status { get; internal set; } = OrderStatus.Accepted;
    public long ExpectedWaitSeconds { get; }
    public QueuePatienceProfile Patience { get; }
    public long? ReadyAtSimTime { get; internal set; }
    public long? ServedAtSimTime { get; internal set; }
    public long? CancelledAtSimTime { get; internal set; }
    public OrderCancellationReason? CancellationReason { get; internal set; }
    public long? ActualWaitSeconds => ReadyAtSimTime - AcceptedAtSimTime;
}

using System.Collections.ObjectModel;
using Zest.Domain.Operations;

namespace Zest.Domain;

public sealed class OperationsState
{
    private readonly Dictionary<Guid, OrderState> _orders = [];
    private readonly Dictionary<string, WorkTaskState> _tasks = new(StringComparer.Ordinal);
    private readonly Dictionary<ResourceKey, OperationalResourceState> _resources = [];
    private readonly List<OrderLossEvent> _lossEvents = [];
    private readonly HashSet<Guid> _lostOrderIds = [];
    private readonly LiveInterventionState _interventions = new();

    public IReadOnlyList<Guid> ActiveOrderIds => _orders.Values
        .Where(order => order.Status is not (OrderStatus.Served or OrderStatus.Cancelled))
        .OrderBy(order => order.AcceptedAtSimTime)
        .ThenBy(order => order.Sequence)
        .Select(order => order.OrderId)
        .ToArray();

    public IReadOnlyDictionary<Guid, OrderState> Orders => new ReadOnlyDictionary<Guid, OrderState>(_orders);
    public IReadOnlyDictionary<string, WorkTaskState> Tasks => new ReadOnlyDictionary<string, WorkTaskState>(_tasks);
    public IReadOnlyDictionary<ResourceKey, OperationalResourceState> Resources => new ReadOnlyDictionary<ResourceKey, OperationalResourceState>(_resources);
    public IReadOnlyList<OrderLossEvent> LossEvents => _lossEvents.AsReadOnly();
    public LiveInterventionState Interventions => _interventions;
    public long SchedulerSimTime { get; internal set; }
    internal long NextOrderSequence { get; set; }
    internal long NextTaskSequence { get; set; }

    internal void AddOrder(OrderState order) => _orders.Add(order.OrderId, order);
    internal void AddTask(WorkTaskState task) => _tasks.Add(task.TaskId, task);
    internal void AddResource(OperationalResourceState resource) => _resources.Add(resource.Key, resource);
    internal bool RecordLoss(OrderLossEvent loss)
    {
        if (!_lostOrderIds.Add(loss.OrderId)) return false;
        _lossEvents.Add(loss);
        return true;
    }

    /// <summary>Drops any live-day orders/tasks/reservation-acquisitions so a mid-day reload starts with an empty queue.</summary>
    public void ClearActiveWork()
    {
        Guid[] activeOrderIds = _orders.Values
            .Where(o => o.Status is not (OrderStatus.Served or OrderStatus.Cancelled))
            .Select(o => o.OrderId).ToArray();
        foreach (Guid id in activeOrderIds)
        {
            OrderState order = _orders[id];
            foreach (string taskId in order.TaskIds)
                if (_tasks.TryGetValue(taskId, out WorkTaskState? task))
                {
                    foreach (var resource in _resources.Values) resource.Release(task.TaskId);
                    _tasks.Remove(taskId);
                }
            _orders.Remove(id);
        }
        NextOrderSequence = 0;
        NextTaskSequence = 0;
        SchedulerSimTime = 0;
    }
}

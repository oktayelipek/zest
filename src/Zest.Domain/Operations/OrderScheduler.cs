using Zest.Domain.Products;

namespace Zest.Domain.Operations;

/// <summary>Deterministic FIFO scheduler for authoritative operational work.</summary>
public sealed class OrderScheduler
{
    private readonly OperationsState _state;

    public OrderScheduler(OperationsState state) =>
        _state = state ?? throw new ArgumentNullException(nameof(state));

    public void RegisterResource(ResourceKind kind, string id, int capacity = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        ResourceKey key = new(kind, id);
        if (_state.Resources.ContainsKey(key)) throw new InvalidOperationException($"Resource '{key}' is already registered.");
        _state.AddResource(new OperationalResourceState(key, capacity));
    }

    public OrderState AcceptOrder(Guid orderId, RecipeVersion recipe, long acceptedAtSimTime, string? customerId = null, QueuePatienceProfile? patience = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (recipe.WorkSteps.Count == 0) throw new ArgumentException("Recipe must define at least one work step.", nameof(recipe));
        return AcceptOrderWithWorkPlan(orderId, recipe, recipe.WorkSteps, acceptedAtSimTime, customerId, patience);
    }

    public OrderState AcceptOrderWithWorkPlan(Guid orderId, RecipeVersion recipe, IReadOnlyList<RecipeWorkStep> workPlan, long acceptedAtSimTime, string? customerId = null, QueuePatienceProfile? patience = null)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
        ArgumentNullException.ThrowIfNull(recipe);
        if (acceptedAtSimTime < _state.SchedulerSimTime) throw new ArgumentOutOfRangeException(nameof(acceptedAtSimTime));
        if (_state.Orders.ContainsKey(orderId)) throw new InvalidOperationException($"Order '{orderId}' already exists.");
        ArgumentNullException.ThrowIfNull(workPlan);
        if (customerId is not null) ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        QueuePatienceProfile effectivePatience = patience ?? new QueuePatienceProfile();
        ValidatePatience(effectivePatience);
        ValidateStepResources(workPlan);

        AdvanceTo(acceptedAtSimTime);
        long orderSequence = ++_state.NextOrderSequence;
        List<string> taskIds = [];
        for (int index = 0; index < workPlan.Count; index++)
        {
            string taskId = $"{orderId:N}:{index}";
            taskIds.Add(taskId);
            _state.AddTask(new WorkTaskState(taskId, orderId, index, workPlan[index], ++_state.NextTaskSequence));
        }

        long expectedWait = EstimateWait(workPlan);
        OrderState order = new(orderId, recipe.Id, customerId, acceptedAtSimTime, orderSequence, taskIds, expectedWait, effectivePatience);
        _state.AddOrder(order);
        if (taskIds.Count == 0)
        {
            order.Status = OrderStatus.Ready;
            order.ReadyAtSimTime = acceptedAtSimTime;
        }
        else
        {
            QueueTask(_state.Tasks[taskIds[0]], acceptedAtSimTime);
            order.Status = OrderStatus.Queued;
            ScheduleAvailable(acceptedAtSimTime);
        }
        return order;
    }

    public void AdvanceTo(long targetSimTime)
    {
        if (targetSimTime < _state.SchedulerSimTime) throw new ArgumentOutOfRangeException(nameof(targetSimTime));
        long cursor = _state.SchedulerSimTime;
        ScheduleAvailable(cursor);

        while (true)
        {
            long? nextCompletion = _state.Tasks.Values
                .Where(task => task.Status == WorkTaskStatus.Running && task.CompletedAtSimTime <= targetSimTime)
                .Min(task => task.CompletedAtSimTime);
            if (nextCompletion is null) break;
            cursor = nextCompletion.Value;
            CompleteDueTasks(cursor);
            ScheduleAvailable(cursor);
        }

        _state.SchedulerSimTime = targetSimTime;
    }

    public bool ServeOrder(Guid orderId, long servedAtSimTime)
    {
        AdvanceTo(servedAtSimTime);
        if (!_state.Orders.TryGetValue(orderId, out OrderState? order)) throw new KeyNotFoundException($"Unknown order '{orderId}'.");
        if (order.Status != OrderStatus.Ready) return false;
        order.Status = OrderStatus.Served;
        order.ServedAtSimTime = servedAtSimTime;
        return true;
    }

    public bool CancelOrder(Guid orderId, long cancelledAtSimTime, OrderCancellationReason reason)
    {
        AdvanceTo(cancelledAtSimTime);
        if (!_state.Orders.TryGetValue(orderId, out OrderState? order)) throw new KeyNotFoundException($"Unknown order '{orderId}'.");
        if (order.Status is not (OrderStatus.Accepted or OrderStatus.Queued or OrderStatus.InProgress)) return false;

        foreach (string taskId in order.TaskIds)
        {
            WorkTaskState task = _state.Tasks[taskId];
            if (task.Status == WorkTaskStatus.Running)
                foreach (ResourceKey key in task.RequiredResources) _state.Resources[key].Release(task.TaskId);
            if (task.Status != WorkTaskStatus.Completed)
            {
                task.Status = WorkTaskStatus.Cancelled;
                task.CompletedAtSimTime = null;
            }
        }
        order.Status = OrderStatus.Cancelled;
        order.CancelledAtSimTime = cancelledAtSimTime;
        order.CancellationReason = reason;
        ScheduleAvailable(cancelledAtSimTime);
        return true;
    }

    private void CompleteDueTasks(long simTime)
    {
        WorkTaskState[] completed = _state.Tasks.Values
            .Where(task => task.Status == WorkTaskStatus.Running && task.CompletedAtSimTime == simTime)
            .OrderBy(task => task.Sequence)
            .ToArray();
        foreach (WorkTaskState task in completed)
        {
            foreach (ResourceKey key in task.RequiredResources) _state.Resources[key].Release(task.TaskId);
            task.Status = WorkTaskStatus.Completed;
            OrderState order = _state.Orders[task.OrderId];
            int nextIndex = task.StepIndex + 1;
            if (nextIndex < order.TaskIds.Count)
            {
                QueueTask(_state.Tasks[order.TaskIds[nextIndex]], simTime);
                order.Status = OrderStatus.Queued;
            }
            else
            {
                order.Status = OrderStatus.Ready;
                order.ReadyAtSimTime = simTime;
            }
        }
    }

    private void ScheduleAvailable(long simTime)
    {
        foreach (WorkTaskState task in _state.Tasks.Values
                     .Where(task => task.Status == WorkTaskStatus.Queued)
                     .OrderBy(task => task.EnqueuedAtSimTime)
                     .ThenBy(task => task.Sequence))
        {
            OperationalResourceState[] resources = task.RequiredResources.Select(key => _state.Resources[key]).ToArray();
            if (resources.Any(resource => !resource.HasCapacity)) continue;
            foreach (OperationalResourceState resource in resources) resource.Acquire(task.TaskId);
            task.Status = WorkTaskStatus.Running;
            task.StartedAtSimTime = simTime;
            task.CompletedAtSimTime = checked(simTime + task.Step.DurationSeconds);
            _state.Orders[task.OrderId].Status = OrderStatus.InProgress;
        }
    }

    private long EstimateWait(IReadOnlyList<RecipeWorkStep> steps)
    {
        long queuedWork = _state.Tasks.Values
            .Where(task => task.Status is WorkTaskStatus.Queued or WorkTaskStatus.Running)
            .Sum(task => (long)task.Step.DurationSeconds);
        return checked(queuedWork + steps.Sum(step => (long)step.DurationSeconds));
    }

    private void ValidateStepResources(IEnumerable<RecipeWorkStep> steps)
    {
        foreach (RecipeWorkStep step in steps)
        {
            if (step.DurationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(steps), "Step duration must be positive.");
            ResourceKey[] required =
            [
                new(ResourceKind.Station, step.StationId),
                new(ResourceKind.Equipment, step.EquipmentId),
                new(ResourceKind.Staff, step.StaffId),
            ];
            ResourceKey? missing = required.Cast<ResourceKey?>().FirstOrDefault(key => !_state.Resources.ContainsKey(key!.Value));
            if (missing is not null) throw new InvalidOperationException($"Required resource '{missing}' is not registered.");
        }
    }

    private static void QueueTask(WorkTaskState task, long simTime)
    {
        task.Status = WorkTaskStatus.Queued;
        task.EnqueuedAtSimTime = simTime;
    }

    private static void ValidatePatience(QueuePatienceProfile profile)
    {
        if (profile.PatienceSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(profile), "Patience must be positive.");
        if (profile.Shape <= 0) throw new ArgumentOutOfRangeException(nameof(profile), "Shape must be positive.");
        if (profile.UnexpectedDelayWeight < 0) throw new ArgumentOutOfRangeException(nameof(profile), "Unexpected delay weight cannot be negative.");
        if (profile.MaximumProbability is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(profile), "Maximum probability must be in (0, 1].");
    }
}

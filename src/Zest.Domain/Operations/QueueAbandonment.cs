using Zest.Domain.Customers;
using Zest.Domain.Inventory;
using Zest.Domain.Randomness;

namespace Zest.Domain.Operations;

public enum AbandonmentOutcome { NotEligible, Stayed, Abandoned }

public sealed record AbandonmentResult(
    Guid OrderId,
    AbandonmentOutcome Outcome,
    long WaitSeconds,
    decimal Probability,
    double DeterministicRoll,
    bool ReservationReleased);

public static class AbandonmentModel
{
    public static decimal Probability(long waitSeconds, long expectedWaitSeconds, QueuePatienceProfile profile)
    {
        if (waitSeconds < 0) throw new ArgumentOutOfRangeException(nameof(waitSeconds));
        if (expectedWaitSeconds < 0) throw new ArgumentOutOfRangeException(nameof(expectedWaitSeconds));
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.PatienceSeconds <= 0 || profile.Shape <= 0 || profile.UnexpectedDelayWeight < 0 || profile.MaximumProbability is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(profile));

        double patience = profile.PatienceSeconds;
        double normalizedWait = waitSeconds / patience;
        double surprise = Math.Max(0, waitSeconds - expectedWaitSeconds) / patience * (double)profile.UnexpectedDelayWeight;
        double pressure = normalizedWait + surprise;
        double cumulative = 1d - Math.Exp(-Math.Pow(pressure, (double)profile.Shape));
        return decimal.Clamp((decimal)cumulative * profile.MaximumProbability, 0, profile.MaximumProbability);
    }
}

/// <summary>Coordinates deterministic abandonment, workload cancellation, reservation release, and loss telemetry.</summary>
public sealed class QueueAbandonmentService
{
    private readonly GameState _gameState;
    private readonly OrderScheduler _scheduler;
    private readonly InventoryService _inventory;

    public QueueAbandonmentService(GameState gameState, OrderScheduler scheduler)
    {
        _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _inventory = new InventoryService(gameState.Inventory);
    }

    public AbandonmentResult Evaluate(Guid orderId, long simTime)
    {
        _scheduler.AdvanceTo(simTime);
        if (!_gameState.Operations.Orders.TryGetValue(orderId, out OrderState? order))
            throw new KeyNotFoundException($"Unknown order '{orderId}'.");
        long wait = checked(simTime - order.AcceptedAtSimTime);
        if (order.Status is not (OrderStatus.Queued or OrderStatus.InProgress))
            return new(orderId, AbandonmentOutcome.NotEligible, Math.Max(0, wait), 0, 0, false);

        decimal probability = AbandonmentModel.Probability(wait, order.ExpectedWaitSeconds, order.Patience);
        double roll = DeterministicRandomFactory.Create(_gameState, RngStreamNames.QueueAbandonment, orderId.ToString("N")).NextDouble();
        if ((decimal)roll >= probability)
            return new(orderId, AbandonmentOutcome.Stayed, wait, probability, roll, false);

        string reservationId = orderId.ToString("N");
        if (!_inventory.CanRelease(reservationId))
            throw new InvalidOperationException($"Order '{orderId}' cannot abandon without an active inventory reservation.");
        if (!_scheduler.CancelOrder(orderId, simTime, OrderCancellationReason.CustomerAbandoned))
            return new(orderId, AbandonmentOutcome.NotEligible, wait, probability, roll, false);
        if (!_inventory.Release(reservationId))
            throw new InvalidOperationException($"Reservation release failed for abandoned order '{orderId}'.");

        bool recorded = _gameState.Operations.RecordLoss(new(
            orderId, order.CustomerId, LostSaleReason.QueueAbandonment,
            _gameState.DayIndex, simTime, wait, probability));
        if (!recorded) throw new InvalidOperationException($"Duplicate queue loss event for order '{orderId}'.");
        return new(orderId, AbandonmentOutcome.Abandoned, wait, probability, roll, true);
    }
}

using Zest.Domain;
using Zest.Domain.Customers;

namespace Zest.Telemetry;

public sealed record QueueLossSummary(int DayIndex, int Count, decimal AverageWaitSeconds, decimal AverageProbability);

public static class QueueLossReporter
{
    public static QueueLossSummary Build(OperationsState state, int dayIndex)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (dayIndex < 0) throw new ArgumentOutOfRangeException(nameof(dayIndex));
        var losses = state.LossEvents
            .Where(loss => loss.DayIndex == dayIndex && loss.Reason == LostSaleReason.QueueAbandonment)
            .ToArray();
        return losses.Length == 0
            ? new(dayIndex, 0, 0, 0)
            : new(dayIndex, losses.Length, losses.Average(loss => (decimal)loss.WaitSeconds), losses.Average(loss => loss.Probability));
    }
}

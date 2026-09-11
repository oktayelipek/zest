using Zest.Domain;
using Zest.Domain.Operations;

namespace Zest.Telemetry;

public sealed record InterventionTraceSummary(
    int InterventionCount,
    int TriggerCount,
    int ActionCount,
    int ConsequenceCount,
    int CoverageLossCount,
    int WastedServingCount);

public static class InterventionTraceReporter
{
    public static InterventionTraceSummary Build(OperationsState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new(
            state.Interventions.Trace.Select(item => item.InterventionId).Where(id => id != Guid.Empty).Distinct().Count(),
            state.Interventions.Trace.Count(item => item.Stage == InterventionTraceStage.Trigger),
            state.Interventions.Trace.Count(item => item.Stage == InterventionTraceStage.Action),
            state.Interventions.Trace.Count(item => item.Stage == InterventionTraceStage.Consequence),
            state.Interventions.CoverageLosses.Count,
            state.Interventions.PreparedBatches.Sum(batch => batch.WastedServings));
    }
}

using Zest.Domain.Customers;
using Zest.Domain.DayCycle;
using Zest.Domain.Diagnostics;
using Zest.Domain.Operations;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class OperationalDiagnosticTests
{
    [Fact]
    public void ObservationProducesReadableSignalsWithoutMutatingSimulation()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        DayCommandProcessor commands = new(state);
        commands.Execute(new PlanDayCommand(350, 2));
        commands.Execute(new StartLiveCommand());
        state.Operations.RecordLoss(new(Guid.NewGuid(), "guest-1", LostSaleReason.PoorProductFit, 0, 10, 25, .5m));
        int ledgerBefore = state.Business.Ledger.Count;
        int traceBefore = state.Operations.Interventions.Trace.Count;
        long timeBefore = state.SimTime;

        OperationalDiagnosticSnapshot result = new OperationalDiagnosticService().Observe(state);

        Assert.Equal(["QUEUE", "PRICE", "STOCK", "PRODUCT FIT"], result.Signals.Select(item => item.Area));
        Assert.Equal(DiagnosticSeverity.Critical, result.Signals.Single(item => item.Area == "STOCK").Severity);
        Assert.Contains("PoorProductFit", Assert.Single(result.LostCustomerFeed));
        Assert.Equal(ledgerBefore, state.Business.Ledger.Count);
        Assert.Equal(traceBefore, state.Operations.Interventions.Trace.Count);
        Assert.Equal(timeBefore, state.SimTime);
    }

    [Fact]
    public void RepricingSurfacesPriceWatchWithoutExposingUtilityFormula()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        new DayCommandProcessor(state).Execute(new StartLiveCommand());
        new LiveInterventionService(state).ChangeLivePrice("classic", 700, 0, 0);

        DiagnosticSignal price = new OperationalDiagnosticService().Observe(state).Signals.Single(item => item.Area == "PRICE");

        Assert.Equal(DiagnosticSeverity.Watch, price.Severity);
        Assert.DoesNotContain("utility", price.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("softmax", price.Evidence, StringComparison.OrdinalIgnoreCase);
    }
}

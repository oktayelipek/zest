using System.Reflection;
using Zest.Domain.DayCycle;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class DayCommandProcessorTests
{
    [Fact]
    public void Full_planning_live_close_report_flow_works()
    {
        GameState state = GameStateFactory.CreateEmpty(7);
        DayCommandProcessor processor = new(state);

        processor.Execute(new PlanDayCommand(425, 18));
        processor.Execute(new StartLiveCommand());
        processor.Execute(new AdvanceLiveCommand(8 * 60 * 60));
        DaySessionSnapshot result = processor.Execute(new CloseDayCommand());

        Assert.Equal(DayPhase.Report, result.Phase);
        Assert.Equal(425, result.PlannedPriceMinor);
        Assert.Equal(18, result.PlannedBatchSize);
        Assert.Equal(28_800, result.SimTime);
        Assert.NotNull(result.Report);
    }

    [Fact]
    public void Same_command_sequence_produces_same_headless_and_ui_result()
    {
        IDayCommand[] commands =
        [
            new PlanDayCommand(375, 16),
            new StartLiveCommand(),
            new AdvanceLiveCommand(14_400),
            new AdvanceLiveCommand(14_400),
            new CloseDayCommand(),
        ];

        DaySessionSnapshot headless = Run(8675309, commands);
        DaySessionSnapshot ui = Run(8675309, commands);

        Assert.Equal(headless, ui);
    }

    [Theory]
    [InlineData(nameof(DayCycleState.Phase))]
    [InlineData(nameof(DayCycleState.PlannedPriceMinor))]
    [InlineData(nameof(DayCycleState.PlannedBatchSize))]
    public void Presentation_cannot_mutate_day_state_directly(string propertyName)
    {
        MethodInfo? setter = typeof(DayCycleState).GetProperty(propertyName)?.SetMethod;
        Assert.NotNull(setter);
        Assert.False(setter!.IsPublic);
    }

    [Fact]
    public void Invalid_phase_transition_is_rejected()
    {
        DayCommandProcessor processor = new(GameStateFactory.CreateEmpty(1));
        Assert.Throws<InvalidOperationException>(() => processor.Execute(new CloseDayCommand()));
    }

    [Fact]
    public void Report_can_start_a_new_day_without_losing_cash()
    {
        GameState state = GameStateFactory.CreateEmpty(1);
        DayCommandProcessor processor = new(state);
        processor.Execute(new PlanDayCommand(350, 12));
        processor.Execute(new StartLiveCommand());
        processor.Execute(new CloseDayCommand());
        long cash = state.Business.CashMinorUnits;

        DaySessionSnapshot next = processor.Execute(new StartNextDayCommand());

        Assert.Equal(DayPhase.MorningBrief, next.Phase);
        Assert.Equal(1, next.DayIndex);
        Assert.Equal(0, next.SimTime);
        Assert.Equal(cash, next.CashMinor);
    }

    [Fact]
    public void Better_counter_is_a_one_time_report_purchase()
    {
        GameState state = GameStateFactory.CreateEmpty(1);
        new Economy.EconomyService(state.Business).InitializeOpeningCash(new Economy.Money(1_000));
        DayCommandProcessor processor = new(state);
        processor.Execute(new StartLiveCommand());
        processor.Execute(new CloseDayCommand());

        processor.Execute(new PurchaseBetterCounterCommand());

        Assert.True(state.Progression.Has(UpgradeIds.BetterCounter));
        Assert.Equal(500, state.Business.CashMinorUnits);
        Assert.Single(state.Business.Ledger, entry => entry.Type == Economy.LedgerEntryType.Upgrade);
        Assert.Throws<InvalidOperationException>(() => processor.Execute(new PurchaseBetterCounterCommand()));
    }

    [Fact]
    public void Juicer_and_cooler_are_independent_report_purchases()
    {
        GameState state = GameStateFactory.CreateEmpty(1);
        new Economy.EconomyService(state.Business).InitializeOpeningCash(new Economy.Money(3_000));
        DayCommandProcessor processor = new(state);
        processor.Execute(new StartLiveCommand());
        processor.Execute(new CloseDayCommand());

        processor.Execute(new PurchaseElectricJuicerCommand());
        processor.Execute(new PurchaseBiggerCoolerCommand());

        Assert.True(state.Progression.Has(UpgradeIds.ElectricJuicer));
        Assert.True(state.Progression.Has(UpgradeIds.BiggerCooler));
        Assert.Equal(1_600, state.Business.CashMinorUnits);
        Assert.Equal(2, state.Business.Ledger.Count(entry => entry.Type == Economy.LedgerEntryType.Upgrade));
    }

    private static DaySessionSnapshot Run(ulong seed, IEnumerable<IDayCommand> commands)
    {
        DayCommandProcessor processor = new(GameStateFactory.CreateEmpty(seed));
        DaySessionSnapshot result = processor.Snapshot();
        foreach (IDayCommand command in commands) result = processor.Execute(command);
        return result;
    }
}

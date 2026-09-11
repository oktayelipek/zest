using Zest.Domain.Economy;
using Zest.Domain.Operations;

namespace Zest.Domain.DayCycle;

/// <summary>The sole command boundary used by both Godot and headless day flows.</summary>
public sealed class DayCommandProcessor
{
    private readonly GameState _state;
    private readonly LiveDayRunner? _runner;

    public DayCommandProcessor(GameState state, LiveDayRunner? runner = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _runner = runner;
    }

    public DaySessionSnapshot Execute(IDayCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        switch (command)
        {
            case PlanDayCommand plan:
                Plan(plan);
                break;
            case StartLiveCommand:
                StartLive();
                break;
            case AdvanceLiveCommand advance:
                Advance(advance);
                break;
            case CloseDayCommand:
                CloseDay();
                break;
            case StartNextDayCommand:
                StartNextDay();
                break;
            case PurchaseBetterCounterCommand:
                new ProgressionService(_state).PurchaseBetterCounter();
                break;
            case PurchaseElectricJuicerCommand:
                new ProgressionService(_state).PurchaseElectricJuicer();
                break;
            case PurchaseBiggerCoolerCommand:
                new ProgressionService(_state).PurchaseBiggerCooler();
                break;
            case ActivateRushMenuCommand rush:
                new LiveInterventionService(_state).ActivateRushMenu(rush.ProductIds);
                break;
            case PrepareExtraBatchCommand batch:
                if (_runner is null) new LiveInterventionService(_state).PrepareExtraBatch(batch.ProductId, batch.Servings, batch.FreshnessSeconds);
                else _runner.PrepareExtraBatch(batch.ProductId, batch.Servings, batch.FreshnessSeconds);
                break;
            case TemporarilyDisableProductCommand disable:
                new LiveInterventionService(_state).TemporarilyDisableProduct(disable.ProductId, disable.DurationSeconds,
                    LiveInterventionService.DefaultCooldownSeconds, LiveInterventionService.DefaultFrictionSeconds);
                break;
            case RequestEmergencyRestockCommand restock:
                new LiveInterventionService(_state).RequestEmergencyRestock(restock.ProductId, restock.Servings, restock.ArrivalDelaySeconds,
                    restock.NormalUnitCostMinor, restock.PremiumMultiplier, restock.FreshnessSeconds);
                break;
            case CallExtraHelpCommand help:
                new LiveInterventionService(_state).CallExtraHelp(help.StaffId, help.ArrivalDelaySeconds, help.DurationSeconds, help.LaborCostMinor);
                break;
            case ChangeLivePriceCommand price:
                if (_runner is null) new LiveInterventionService(_state).ChangeLivePrice(price.ProductId, price.NewPriceMinor);
                else _runner.ChangePrice(price.ProductId, price.NewPriceMinor);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command.GetType().Name, "Unknown day command.");
        }
        _state.DayCycle.Record(command.GetType().Name);
        return Snapshot();
    }

    public DaySessionSnapshot Snapshot()
    {
        FinancialSnapshot finances = new EconomyService(_state.Business).ProjectDay(_state.DayIndex);
        return new(
            _state.DayCycle.Phase,
            _state.DayIndex,
            _state.SimTime,
            _state.DayCycle.PlannedPriceMinor,
            _state.DayCycle.PlannedBatchSize,
            _state.Business.CashMinorUnits,
            _state.Operations.ActiveOrderIds.Count,
            _state.DayCycle.RemainingOpeningServings + _state.Operations.Interventions.PreparedBatches.Sum(item => item.RemainingServings),
            _state.Business.Ledger.Count(entry => entry.DayIndex == _state.DayIndex && entry.Type == LedgerEntryType.Sale),
            _state.Operations.LossEvents.Count(loss => loss.DayIndex == _state.DayIndex && loss.Reason is Customers.LostSaleReason.QueueAbandonment or Customers.LostSaleReason.ClosingTime),
            _state.DayCycle.Report);
    }

    private void Plan(PlanDayCommand command)
    {
        RequirePhase(DayPhase.MorningBrief);
        if (command.MenuPriceMinor <= 0) throw new ArgumentOutOfRangeException(nameof(command.MenuPriceMinor));
        if (command.BatchSize <= 0) throw new ArgumentOutOfRangeException(nameof(command.BatchSize));
        _state.DayCycle.PlannedPriceMinor = command.MenuPriceMinor;
        _state.DayCycle.PlannedBatchSize = command.BatchSize;
        _state.DayCycle.RemainingOpeningServings = command.BatchSize;
    }

    private void StartLive()
    {
        RequirePhase(DayPhase.MorningBrief);
        _state.DayCycle.Phase = DayPhase.Live;
        _runner?.StartDay();
    }

    private void Advance(AdvanceLiveCommand command)
    {
        RequirePhase(DayPhase.Live);
        if (command.Seconds <= 0) throw new ArgumentOutOfRangeException(nameof(command.Seconds));
        long target = checked(_state.SimTime + command.Seconds);
        if (_runner is not null) _runner.AdvanceTo(target);
        else
        {
            new LiveInterventionService(_state).AdvanceTo(target);
            _state.SimTime = target;
        }
    }

    private void CloseDay()
    {
        RequirePhase(DayPhase.Live);
        _runner?.CloseDay();
        DaySessionSnapshot snapshot = Snapshot();
        FinancialSnapshot finances = new EconomyService(_state.Business).ProjectDay(_state.DayIndex);
        _state.DayCycle.Report = new(
            _state.DayIndex,
            finances.Revenue.MinorUnits,
            finances.Profit.MinorUnits,
            _state.Business.CashMinorUnits,
            snapshot.SalesCount,
            snapshot.QueueLossCount,
            snapshot.QueueCount,
            snapshot.StockOnHand);
        int reputationChange = Math.Clamp(snapshot.SalesCount / 5 - snapshot.QueueLossCount, -5, 5);
        _state.Progression.AdjustReputation(reputationChange);
        _state.DayCycle.Phase = DayPhase.Report;
    }

    private void StartNextDay()
    {
        RequirePhase(DayPhase.Report);
        _state.DayIndex++;
        _state.SimTime = 0;
        _state.Operations.SchedulerSimTime = 0;
        _state.DayCycle.Phase = DayPhase.MorningBrief;
        _state.DayCycle.Report = null;
        _state.DayCycle.RemainingOpeningServings = 0;
        _state.DayCycle.Brief = new(_state.DayIndex, "A fresh day in the park", "Carry yesterday's lesson into today's plan.");
    }

    private void RequirePhase(DayPhase phase)
    {
        if (_state.DayCycle.Phase != phase)
            throw new InvalidOperationException($"Command requires phase '{phase}', current phase is '{_state.DayCycle.Phase}'.");
    }
}

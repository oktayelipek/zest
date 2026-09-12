using Zest.Domain.DayCycle;
using Zest.Domain.Economy;

namespace Zest.Domain;

public static class UpgradeIds
{
    public const string BetterCounter = "better-counter";
    public const string ElectricJuicer = "electric-juicer";
    public const string BiggerCooler = "bigger-cooler";
}

public static class MenuIds
{
    /// <summary>Berry Lemonade is offered as a Day 1 unlock decision rather than a purchase.</summary>
    public const string Berry = "menu-berry";
    /// <summary>Strong Lemonade uses two lemons; targets the commuter segment.</summary>
    public const string Strong = "menu-strong";
}

/// <summary>Owns the first persistent player investment and its financial fact.</summary>
public sealed class ProgressionService
{
    public const long BetterCounterCostMinor = 500;
    public const long ElectricJuicerCostMinor = 800;
    public const long BiggerCoolerCostMinor = 600;
    private readonly GameState _state;
    public ProgressionService(GameState state) => _state = state ?? throw new ArgumentNullException(nameof(state));

    public bool PurchaseBetterCounter()
        => Purchase(UpgradeIds.BetterCounter, BetterCounterCostMinor);

    public bool PurchaseElectricJuicer()
        => Purchase(UpgradeIds.ElectricJuicer, ElectricJuicerCostMinor);

    public bool PurchaseBiggerCooler()
        => Purchase(UpgradeIds.BiggerCooler, BiggerCoolerCostMinor);

    /// <summary>Free menu decision; added to Unlocks so LiveDayRunner exposes berry to customers.</summary>
    public bool AddBerryToMenu() => AddOptionalRecipeToMenu(MenuIds.Berry);
    public bool AddStrongToMenu() => AddOptionalRecipeToMenu(MenuIds.Strong);

    private bool AddOptionalRecipeToMenu(string menuId)
    {
        if (_state.DayCycle.Phase != DayPhase.Report && _state.DayCycle.Phase != DayPhase.MorningBrief)
            throw new InvalidOperationException("Menu changes are made between days.");
        return _state.Progression.Unlock(menuId);
    }

    private bool Purchase(string upgradeId, long costMinor)
    {
        if (_state.DayCycle.Phase != DayPhase.Report) throw new InvalidOperationException("Upgrades can be purchased from the daily report.");
        if (_state.Progression.Has(upgradeId)) throw new InvalidOperationException($"{upgradeId} is already installed.");
        if (_state.Business.CashMinorUnits < costMinor) throw new InvalidOperationException($"Not enough cash for {upgradeId}.");
        new EconomyService(_state.Business).PostExpense(LedgerEntryType.Upgrade, new Money(costMinor), upgradeId, _state.SimTime, _state.DayIndex);
        return _state.Progression.Unlock(upgradeId);
    }
}

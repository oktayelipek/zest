namespace Zest.Domain.DayCycle;

public interface IDayCommand;

public sealed record PlanDayCommand(long MenuPriceMinor, int BatchSize) : IDayCommand;
public sealed record StartLiveCommand : IDayCommand;
public sealed record AdvanceLiveCommand(long Seconds) : IDayCommand;
public sealed record CloseDayCommand : IDayCommand;
public sealed record StartNextDayCommand : IDayCommand;
public sealed record PurchaseBetterCounterCommand : IDayCommand;
public sealed record PurchaseElectricJuicerCommand : IDayCommand;
public sealed record PurchaseBiggerCoolerCommand : IDayCommand;
public sealed record AddBerryToMenuCommand : IDayCommand;
public sealed record AddStrongToMenuCommand : IDayCommand;
public sealed record ActivateRushMenuCommand(IReadOnlyList<string> ProductIds) : IDayCommand;
public sealed record PrepareExtraBatchCommand(string ProductId, int Servings, int FreshnessSeconds) : IDayCommand;
public sealed record TemporarilyDisableProductCommand(string ProductId, int DurationSeconds = 900) : IDayCommand;
public sealed record RequestEmergencyRestockCommand(string ProductId, int Servings, int ArrivalDelaySeconds, long NormalUnitCostMinor, decimal PremiumMultiplier = 1.5m, int FreshnessSeconds = 1800) : IDayCommand;
public sealed record CallExtraHelpCommand(string StaffId, int ArrivalDelaySeconds, int DurationSeconds, long LaborCostMinor) : IDayCommand;
public sealed record ChangeLivePriceCommand(string ProductId, long NewPriceMinor) : IDayCommand;

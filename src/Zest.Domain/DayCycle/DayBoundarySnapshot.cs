using Zest.Domain.Economy;

namespace Zest.Domain.DayCycle;

/// <summary>A stable save contract for the safe morning boundary between days.</summary>
public sealed record DayBoundarySnapshot(
    int SaveVersion,
    ulong RootSeed,
    string ConfigVersion,
    int DayIndex,
    long OpeningCashMinor,
    long CashMinor,
    long PlannedPriceMinor,
    int PlannedBatchSize,
    int Reputation,
    IReadOnlyList<string> Unlocks,
    IReadOnlyList<InventoryLotSnapshot> Inventory,
    IReadOnlyList<LedgerEntry> Ledger)
{
    public const int CurrentSaveVersion = 2;
}

public sealed record InventoryLotSnapshot(string LotId, string IngredientId, decimal RemainingQuantity, DateOnly? ExpiresOn);

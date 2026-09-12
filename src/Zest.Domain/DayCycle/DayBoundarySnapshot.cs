using Zest.Domain.Economy;

namespace Zest.Domain.DayCycle;

/// <summary>Save contract for morning boundary and mid-day quicksave.
/// v3 adds SimTime, Phase and live intervention state (prepared batches, prices, disabled products, rush menu).
/// Active orders/tasks/reservations are intentionally NOT persisted; a mid-day reload begins that hour with an empty queue.</summary>
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
    IReadOnlyList<LedgerEntry> Ledger,
    long SimTime = 0,
    string Phase = "MorningBrief",
    int RemainingOpeningServings = 0,
    IReadOnlyList<PreparedBatchSnapshot>? PreparedBatches = null,
    IReadOnlyDictionary<string, long>? CurrentPrices = null,
    IReadOnlyDictionary<string, long>? DisabledUntil = null,
    IReadOnlyList<string>? RushMenuIds = null)
{
    public const int CurrentSaveVersion = 3;
}

public sealed record InventoryLotSnapshot(string LotId, string IngredientId, decimal RemainingQuantity, DateOnly? ExpiresOn);
public sealed record PreparedBatchSnapshot(Guid BatchId, string ProductId, int InitialServings, int RemainingServings, long PreparedAtSimTime, long ExpiresAtSimTime, long PrepaidCostMinor);

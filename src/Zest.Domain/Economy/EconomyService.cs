using Zest.Domain.Products;

namespace Zest.Domain.Economy;

/// <summary>Owns all append-only ledger writes and the derived cash balance.</summary>
public sealed class EconomyService
{
    private readonly BusinessState _state;

    public EconomyService(BusinessState state) =>
        _state = state ?? throw new ArgumentNullException(nameof(state));

    public void InitializeOpeningCash(Money openingCash)
    {
        if (_state.Ledger.Count != 0)
            throw new InvalidOperationException("Opening cash cannot change after ledger activity begins.");

        _state.OpeningCashMinorUnits = openingCash.MinorUnits;
        _state.CashMinorUnits = openingCash.MinorUnits;
    }

    /// <summary>Posts both revenue and COGS atomically and at most once per served order.</summary>
    public bool PostServedOrder(string orderId, RecipeVersion recipe, long simTime, int dayIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentNullException.ThrowIfNull(recipe);
        ValidateCoordinates(simTime, dayIndex);
        if (_state.HasServedOrder(orderId)) return false;
        if (recipe.SalePriceMinor < 0 || recipe.CogsMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(recipe), "Sale price and COGS cannot be negative.");

        LedgerEntry sale = Create(simTime, dayIndex, LedgerEntryType.Sale,
            new Money(recipe.SalePriceMinor), orderId);
        LedgerEntry cogs = Create(simTime, dayIndex, LedgerEntryType.CostOfGoodsSold,
            new Money(checked(-recipe.CogsMinor)), orderId);
        Money cashDelta = sale.Amount + cogs.Amount;
        long resultingCash = checked(_state.CashMinorUnits + cashDelta.MinorUnits);

        _state.Append(sale);
        _state.Append(cogs);
        _state.RecordServedOrder(orderId);
        _state.CashMinorUnits = resultingCash;
        return true;
    }

    public LedgerEntry PostExpense(
        LedgerEntryType type,
        Money cost,
        string referenceId,
        long simTime,
        int dayIndex,
        bool affectsCash = true)
    {
        if (type is not (LedgerEntryType.Waste or LedgerEntryType.Labor or LedgerEntryType.Rent or LedgerEntryType.Supply or LedgerEntryType.Upgrade))
            throw new ArgumentOutOfRangeException(nameof(type), "Use PostServedOrder for sale and COGS entries.");
        if (cost.MinorUnits < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);
        ValidateCoordinates(simTime, dayIndex);

        LedgerEntry entry = Create(simTime, dayIndex, type, -cost, referenceId, affectsCash);
        long resultingCash = affectsCash
            ? checked(_state.CashMinorUnits + entry.Amount.MinorUnits)
            : _state.CashMinorUnits;
        _state.Append(entry);
        _state.CashMinorUnits = resultingCash;
        return entry;
    }

    public FinancialSnapshot ProjectDay(int dayIndex)
    {
        if (dayIndex < 0) throw new ArgumentOutOfRangeException(nameof(dayIndex));
        return FinancialSnapshot.From(_state.Ledger.Where(entry => entry.DayIndex == dayIndex));
    }

    public CashReconciliation ReconcileCash()
    {
        long ledgerCash = _state.Ledger.Where(entry => entry.AffectsCash)
            .Sum(entry => entry.Amount.MinorUnits);
        return new(new Money(checked(_state.OpeningCashMinorUnits + ledgerCash)), new Money(_state.CashMinorUnits));
    }

    private LedgerEntry Create(long simTime, int dayIndex, LedgerEntryType type, Money amount,
        string referenceId, bool affectsCash = true) =>
        new(_state.Ledger.Count + 1L, simTime, dayIndex, type, amount, referenceId, affectsCash);

    private static void ValidateCoordinates(long simTime, int dayIndex)
    {
        if (simTime < 0) throw new ArgumentOutOfRangeException(nameof(simTime));
        if (dayIndex < 0) throw new ArgumentOutOfRangeException(nameof(dayIndex));
    }
}

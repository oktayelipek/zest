namespace Zest.Domain;

public sealed class BusinessState
{
    private readonly List<Economy.LedgerEntry> _ledger = [];
    private readonly HashSet<string> _servedOrderIds = new(StringComparer.Ordinal);

    public long OpeningCashMinorUnits { get; internal set; }

    public long CashMinorUnits { get; internal set; }

    public IReadOnlyList<Economy.LedgerEntry> Ledger => _ledger.AsReadOnly();

    internal bool HasServedOrder(string orderId) => _servedOrderIds.Contains(orderId);

    internal void RecordServedOrder(string orderId) => _servedOrderIds.Add(orderId);

    internal void Append(Economy.LedgerEntry entry) => _ledger.Add(entry);

    internal void Restore(long openingCashMinor, long cashMinor, IEnumerable<Economy.LedgerEntry> ledger)
    {
        OpeningCashMinorUnits = openingCashMinor;
        CashMinorUnits = cashMinor;
        _ledger.Clear();
        _servedOrderIds.Clear();
        foreach (Economy.LedgerEntry entry in ledger.OrderBy(entry => entry.Sequence))
        {
            _ledger.Add(entry);
            if (entry.Type == Economy.LedgerEntryType.Sale) _servedOrderIds.Add(entry.ReferenceId);
        }
    }
}

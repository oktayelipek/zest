namespace Zest.Domain.Economy;

public sealed record FinancialSnapshot(
    Money Revenue,
    Money Cogs,
    Money Waste,
    Money Labor,
    Money Rent,
    Money Supply,
    Money Profit)
{
    public static FinancialSnapshot From(IEnumerable<LedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        LedgerEntry[] facts = entries.ToArray();
        Money revenue = Sum(facts, LedgerEntryType.Sale);
        Money cogs = -Sum(facts, LedgerEntryType.CostOfGoodsSold);
        Money waste = -Sum(facts, LedgerEntryType.Waste);
        Money labor = -Sum(facts, LedgerEntryType.Labor);
        Money rent = -Sum(facts, LedgerEntryType.Rent);
        Money supply = -Sum(facts, LedgerEntryType.Supply);
        return new(revenue, cogs, waste, labor, rent, supply,
            new Money(checked(revenue.MinorUnits - cogs.MinorUnits - waste.MinorUnits - labor.MinorUnits - rent.MinorUnits - supply.MinorUnits)));
    }

    private static Money Sum(IEnumerable<LedgerEntry> entries, LedgerEntryType type) =>
        new(entries.Where(entry => entry.Type == type).Sum(entry => entry.Amount.MinorUnits));
}

public sealed record CashReconciliation(Money Expected, Money Actual)
{
    public Money Difference => Actual - Expected;
    public bool IsBalanced => Difference == Money.Zero;
}

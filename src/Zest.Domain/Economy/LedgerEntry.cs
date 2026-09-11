namespace Zest.Domain.Economy;

public enum LedgerEntryType
{
    Sale,
    CostOfGoodsSold,
    Waste,
    Labor,
    Rent,
    Supply,
    Upgrade,
}

/// <summary>
/// An immutable financial fact. Amount is signed: income is positive and costs are negative.
/// </summary>
public sealed record LedgerEntry(
    long Sequence,
    long SimTime,
    int DayIndex,
    LedgerEntryType Type,
    Money Amount,
    string ReferenceId,
    bool AffectsCash = true);

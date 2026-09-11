namespace Zest.Domain.Economy;

/// <summary>A currency minor-unit value. Zest's economy never uses floating point.</summary>
public readonly record struct Money(long MinorUnits)
{
    public static Money Zero => new(0);

    public static Money operator +(Money left, Money right) =>
        new(checked(left.MinorUnits + right.MinorUnits));

    public static Money operator -(Money value) => new(checked(-value.MinorUnits));

    public static Money operator -(Money left, Money right) => left + -right;
}

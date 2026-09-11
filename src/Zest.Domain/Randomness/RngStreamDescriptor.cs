using System.Globalization;

namespace Zest.Domain.Randomness;

public sealed record RngStreamDescriptor(
    ulong RootSeed,
    int DayIndex,
    string StreamName,
    string EntityId,
    ulong EffectiveSeed)
{
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"rootSeed={RootSeed}; day={DayIndex}; stream={StreamName}; entity={EntityId}; effectiveSeed=0x{EffectiveSeed:X16}");
}


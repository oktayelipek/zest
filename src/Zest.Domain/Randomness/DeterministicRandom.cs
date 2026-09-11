namespace Zest.Domain.Randomness;

/// <summary>SplitMix64 generator with a stable, explicitly versioned algorithm.</summary>
public sealed class DeterministicRandom
{
    private ulong _state;

    internal DeterministicRandom(RngStreamDescriptor descriptor)
    {
        Descriptor = descriptor;
        _state = descriptor.EffectiveSeed;
    }

    public RngStreamDescriptor Descriptor { get; }

    public ulong DrawCount { get; private set; }

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        value ^= value >> 31;
        DrawCount++;
        return value;
    }

    public int NextInt(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        }

        // Rejection sampling avoids modulo bias while preserving deterministic draw order.
        ulong bound = (ulong)exclusiveMax;
        ulong threshold = unchecked(0UL - bound) % bound;
        while (true)
        {
            ulong value = NextUInt64();
            if (value >= threshold)
            {
                return (int)(value % bound);
            }
        }
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));
}


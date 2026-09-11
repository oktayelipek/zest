using System.Buffers.Binary;
using System.Text;

namespace Zest.Domain.Randomness;

/// <summary>Stable seed derivation; deliberately avoids runtime-dependent string hashes.</summary>
public static class DeterministicSeed
{
    private const ulong FnvOffsetBasis = 14_695_981_039_346_656_037UL;
    private const ulong FnvPrime = 1_099_511_628_211UL;

    public static ulong Derive(
        ulong rootSeed,
        int dayIndex,
        string streamName,
        string? entityId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);
        if (dayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayIndex));
        }

        string stableEntityId = entityId ?? string.Empty;
        ulong hash = FnvOffsetBasis;

        Span<byte> numericBuffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(numericBuffer, rootSeed);
        AddBytes(ref hash, numericBuffer);

        BinaryPrimitives.WriteInt32LittleEndian(numericBuffer, dayIndex);
        AddBytes(ref hash, numericBuffer[..4]);
        AddString(ref hash, streamName);
        AddString(ref hash, stableEntityId);

        return Avalanche(hash);
    }

    private static void AddString(ref ulong hash, string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        Span<byte> lengthBuffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, byteCount);
        AddBytes(ref hash, lengthBuffer);

        byte[] utf8 = Encoding.UTF8.GetBytes(value);
        AddBytes(ref hash, utf8);
    }

    private static void AddBytes(ref ulong hash, ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
        {
            hash ^= value;
            hash *= FnvPrime;
        }
    }

    private static ulong Avalanche(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}


namespace Zest.Domain.Randomness;

public static class DeterministicRandomFactory
{
    public static DeterministicRandom Create(
        GameState state,
        string streamName,
        string? entityId = null) =>
        Create(state.RootSeed, state.DayIndex, streamName, entityId);

    public static DeterministicRandom Create(
        ulong rootSeed,
        int dayIndex,
        string streamName,
        string? entityId = null)
    {
        string stableEntityId = entityId ?? string.Empty;
        ulong effectiveSeed = DeterministicSeed.Derive(rootSeed, dayIndex, streamName, stableEntityId);
        RngStreamDescriptor descriptor = new(
            rootSeed,
            dayIndex,
            streamName,
            stableEntityId,
            effectiveSeed);

        return new DeterministicRandom(descriptor);
    }
}


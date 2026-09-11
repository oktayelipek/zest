using Zest.Domain;

namespace Zest.Config;

public static class ConfigGameStateFactory
{
    public static GameState CreateEmpty(ulong rootSeed, ContentConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return GameStateFactory.CreateEmpty(rootSeed, config.Version);
    }
}

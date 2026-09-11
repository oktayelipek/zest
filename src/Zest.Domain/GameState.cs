namespace Zest.Domain;

/// <summary>
/// The single authoritative root of all mutable simulation state.
/// Presentation code may observe this graph, but mutations belong to the domain assembly.
/// </summary>
public sealed class GameState
{
    public const int CurrentSchemaVersion = 1;

    internal GameState(ulong rootSeed, string configVersion)
    {
        RootSeed = rootSeed;
        ConfigVersion = configVersion;
    }

    public int SchemaVersion { get; } = CurrentSchemaVersion;

    public ulong RootSeed { get; }

    public string ConfigVersion { get; }

    public int DayIndex { get; internal set; }

    public long SimTime { get; internal set; }

    public WorldState World { get; } = new();

    public BusinessState Business { get; } = new();

    public CustomerRegistryState Customers { get; } = new();

    public OperationsState Operations { get; } = new();

    public InventoryState Inventory { get; } = new();

    public ProgressionState Progression { get; } = new();

    public DayCycle.DayCycleState DayCycle { get; } = new();
}

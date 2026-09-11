using System.Text.Json;
using Zest.Domain;
using Zest.Domain.DayCycle;

namespace Zest.Persistence;

/// <summary>File persistence for complete, safe day-boundary snapshots.</summary>
public sealed class DayBoundarySaveService
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Save(string path, GameState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DayBoundarySnapshot snapshot = GameStateFactory.CreateDayBoundarySnapshot(state);
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, Json));
    }

    public GameState Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DayBoundarySnapshot snapshot = JsonSerializer.Deserialize<DayBoundarySnapshot>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException("Save file does not contain a day-boundary snapshot.");
        return GameStateFactory.RestoreDayBoundary(snapshot);
    }
}

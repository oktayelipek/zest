using System.Text.Json;

namespace Zest.Persistence;

/// <summary>Small, versioned player-facing presentation preferences kept outside gameplay saves.</summary>
public sealed class UserPreferenceService
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public UserPreferences Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) return UserPreferences.Default;
        UserPreferences? stored;
        try
        {
            stored = JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(path), Json);
        }
        catch (Exception error) when (error is IOException or JsonException)
        {
            return UserPreferences.Default;
        }
        if (stored is null || stored.Version > CurrentVersion) return UserPreferences.Default;
        int level = !stored.SoundEnabled ? 0 : Math.Clamp(stored.SoundLevel, 0, 2);
        return stored with { Version = CurrentVersion, TextScale = NormalizeTextScale(stored.TextScale), SoundLevel = level, SoundEnabled = level > 0 };
    }

    public void Save(string path, UserPreferences preferences)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(preferences);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        UserPreferences normalized = preferences with { Version = CurrentVersion, TextScale = NormalizeTextScale(preferences.TextScale) };
        File.WriteAllText(path, JsonSerializer.Serialize(normalized, Json));
    }

    private static float NormalizeTextScale(float value) => float.IsFinite(value) ? Math.Clamp(value, 1f, 1.35f) : 1f;
}

public sealed record UserPreferences(int Version, float TextScale, bool ReducedMotion, bool SoundEnabled = true, int SoundLevel = 2)
{
    public static UserPreferences Default { get; } = new(1, 1f, false);
}

using System.Text.Json.Serialization;

namespace Zest.Config;

public sealed record ConfigManifest
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("files")]
    public required ConfigFileSet Files { get; init; }
}

public sealed record ConfigFileSet
{
    public required string Segments { get; init; }
    public required string Ingredients { get; init; }
    public required string Recipes { get; init; }
    public required string Locations { get; init; }
    public required string Weather { get; init; }
    public required string Stations { get; init; }
    public required string Equipment { get; init; }
    public required string Staff { get; init; }
}

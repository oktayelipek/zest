using Zest.Domain.World;

namespace Zest.Config;

public static class WorldGenerationProfileFactory
{
    public static WorldGenerationProfile Create(ContentConfig config, string locationId)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!config.Locations.TryGetValue(locationId, out LocationDefinition? location))
            throw new KeyNotFoundException($"Unknown location '{locationId}'.");

        decimal[] traffic = location.TrafficCurve.OrderBy(item => item.Hour).Select(item => item.OpportunitiesPerHour).ToArray();
        WeatherGenerationProfile[] weather = config.Weather.Values
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new WeatherGenerationProfile(item.Id, item.Weight, item.ArrivalMultiplier, item.MinTemperatureC, item.MaxTemperatureC))
            .ToArray();
        SegmentGenerationProfile[] segments = config.Segments.Values
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new SegmentGenerationProfile(
                item.Id,
                item.HourlyWeights.OrderBy(point => point.Hour).Select(point => point.Weight).ToArray()))
            .ToArray();

        return new WorldGenerationProfile(
            location.Id,
            location.FootTrafficMultiplier,
            location.WeekdayMultiplier,
            location.WeekendMultiplier,
            traffic,
            weather,
            segments);
    }
}

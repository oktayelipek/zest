namespace Zest.Domain.World;

public sealed record WorldGenerationProfile(
    string LocationId,
    decimal FootTrafficMultiplier,
    decimal WeekdayMultiplier,
    decimal WeekendMultiplier,
    IReadOnlyList<decimal> HourlyOpportunities,
    IReadOnlyList<WeatherGenerationProfile> Weather,
    IReadOnlyList<SegmentGenerationProfile> Segments);

public sealed record WeatherGenerationProfile(
    string Id,
    decimal Weight,
    decimal TrafficMultiplier,
    decimal MinTemperatureC,
    decimal MaxTemperatureC);

public sealed record SegmentGenerationProfile(
    string Id,
    IReadOnlyList<decimal> HourlyWeights);

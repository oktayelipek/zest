using Zest.Domain.Randomness;

namespace Zest.Domain.World;

public sealed class WorldGenerator
{
    public WorldDayContext GenerateDayContext(GameState state, DateOnly date, WorldGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateProfile(profile);

        DeterministicRandom weatherRandom = DeterministicRandomFactory.Create(
            state.RootSeed,
            state.DayIndex,
            RngStreamNames.WorldWeather,
            profile.LocationId);
        WeatherGenerationProfile weather = SelectWeighted(profile.Weather, item => item.Weight, weatherRandom);
        decimal temperature = weather.MinTemperatureC
            + ((weather.MaxTemperatureC - weather.MinTemperatureC) * (decimal)weatherRandom.NextDouble());

        return new WorldDayContext(
            new CalendarTimeState(date, 0),
            weather.Id,
            decimal.Round(temperature, 1, MidpointRounding.ToEven),
            weather.TrafficMultiplier);
    }

    public IReadOnlyList<PasserbyOpportunity> GenerateHour(
        GameState state,
        CalendarTimeState time,
        WorldDayContext day,
        WorldGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateProfile(profile);
        if (time.Hour is < 0 or > 23) throw new ArgumentOutOfRangeException(nameof(time));
        if (time.Date != day.Time.Date) throw new ArgumentException("Hour and day context dates must match.", nameof(time));

        decimal calendarMultiplier = time.IsWeekend ? profile.WeekendMultiplier : profile.WeekdayMultiplier;
        decimal mean = profile.HourlyOpportunities[time.Hour]
            * profile.FootTrafficMultiplier
            * calendarMultiplier
            * day.TrafficMultiplier;

        string hourEntity = $"{profile.LocationId}:{time.Date:yyyyMMdd}:{time.Hour:D2}";
        DeterministicRandom trafficRandom = DeterministicRandomFactory.Create(
            state.RootSeed,
            state.DayIndex,
            RngStreamNames.WorldTraffic,
            hourEntity);
        int count = SamplePoisson(mean, trafficRandom);
        List<PasserbyOpportunity> opportunities = new(count);

        for (int index = 0; index < count; index++)
        {
            string id = $"{hourEntity}:{index:D4}";
            DeterministicRandom segmentRandom = DeterministicRandomFactory.Create(
                state.RootSeed,
                state.DayIndex,
                RngStreamNames.CustomerSpawn,
                id);
            SegmentGenerationProfile segment = SelectWeighted(
                profile.Segments,
                item => item.HourlyWeights[time.Hour],
                segmentRandom);
            opportunities.Add(new PasserbyOpportunity(
                id,
                time,
                profile.LocationId,
                segment.Id,
                day.WeatherId,
                day.TemperatureC));
        }

        return opportunities;
    }

    internal static int SamplePoisson(decimal mean, DeterministicRandom random)
    {
        if (mean < 0) throw new ArgumentOutOfRangeException(nameof(mean));
        if (mean == 0) return 0;
        if (mean > 100) throw new ArgumentOutOfRangeException(nameof(mean), "Hourly mean above 100 is not supported.");

        double limit = Math.Exp(-(double)mean);
        int count = 0;
        double product = 1;
        do
        {
            count++;
            product *= random.NextDouble();
        }
        while (product > limit);

        return count - 1;
    }

    private static T SelectWeighted<T>(IReadOnlyList<T> items, Func<T, decimal> weight, DeterministicRandom random)
    {
        decimal total = items.Sum(weight);
        if (total <= 0) throw new InvalidOperationException("At least one positive selection weight is required.");
        decimal cursor = (decimal)random.NextDouble() * total;
        foreach (T item in items)
        {
            cursor -= weight(item);
            if (cursor < 0) return item;
        }

        return items[^1];
    }

    private static void ValidateProfile(WorldGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.HourlyOpportunities.Count != 24) throw new ArgumentException("Traffic profile must contain 24 hourly values.", nameof(profile));
        if (profile.Weather.Count == 0) throw new ArgumentException("Weather profile cannot be empty.", nameof(profile));
        if (profile.Segments.Count == 0 || profile.Segments.Any(item => item.HourlyWeights.Count != 24))
            throw new ArgumentException("Segment profiles must contain 24 hourly values.", nameof(profile));
    }
}

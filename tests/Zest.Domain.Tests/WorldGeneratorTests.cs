using Zest.Domain.Randomness;
using Zest.Domain.World;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class WorldGeneratorTests
{
    [Fact]
    public void Same_seed_day_and_profile_produce_identical_opportunities()
    {
        WorldGenerationProfile profile = CreateProfile();
        CalendarTimeState time = new(new DateOnly(2026, 9, 8), 12);
        WorldDayContext day = new(time with { Hour = 0 }, "sunny", 24, 1);
        WorldGenerator generator = new();

        IReadOnlyList<PasserbyOpportunity> first = generator.GenerateHour(GameStateFactory.CreateEmpty(42), time, day, profile);
        IReadOnlyList<PasserbyOpportunity> second = generator.GenerateHour(GameStateFactory.CreateEmpty(42), time, day, profile);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Generator_only_emits_opportunities_and_never_mutates_business_state()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        WorldGenerationProfile profile = CreateProfile();
        CalendarTimeState time = new(new DateOnly(2026, 9, 8), 12);
        WorldDayContext day = new(time with { Hour = 0 }, "sunny", 24, 1);

        IReadOnlyList<PasserbyOpportunity> opportunities = new WorldGenerator().GenerateHour(state, time, day, profile);

        Assert.NotEmpty(opportunities);
        Assert.Equal(0, state.Business.CashMinorUnits);
        Assert.Empty(state.Customers.Customers);
        Assert.All(opportunities, item => Assert.Equal("park", item.LocationId));
    }

    [Fact]
    public void Segment_mix_converges_to_configured_weights()
    {
        WorldGenerationProfile profile = CreateProfile(
            segments:
            [
                new SegmentGenerationProfile("student", Repeat(3)),
                new SegmentGenerationProfile("worker", Repeat(1)),
            ]);
        CalendarTimeState time = new(new DateOnly(2026, 9, 8), 12);
        WorldDayContext day = new(time with { Hour = 0 }, "sunny", 24, 1);
        WorldGenerator generator = new();
        List<PasserbyOpportunity> all = [];

        for (ulong seed = 0; seed < 400; seed++)
            all.AddRange(generator.GenerateHour(GameStateFactory.CreateEmpty(seed), time, day, profile));

        double studentShare = all.Count(item => item.SegmentId == "student") / (double)all.Count;
        Assert.InRange(studentShare, 0.72, 0.78);
    }

    [Fact]
    public void Thousand_seed_batch_has_poisson_mean_and_variance()
    {
        const decimal targetMean = 12;
        int[] samples = new int[1_000];
        for (int seed = 0; seed < samples.Length; seed++)
        {
            DeterministicRandom random = DeterministicRandomFactory.Create((ulong)seed, 0, RngStreamNames.WorldTraffic, "fixture");
            samples[seed] = WorldGenerator.SamplePoisson(targetMean, random);
        }

        double mean = samples.Average();
        double variance = samples.Select(value => Math.Pow(value - mean, 2)).Average();

        Assert.InRange(mean, 11.5, 12.5);
        Assert.InRange(variance, 10.5, 13.5);
    }

    [Fact]
    public void Rain_and_weekend_modifiers_change_only_opportunity_pressure()
    {
        WorldGenerationProfile profile = CreateProfile();
        WorldGenerator generator = new();
        GameState weekdayState = GameStateFactory.CreateEmpty(123);
        GameState weekendState = GameStateFactory.CreateEmpty(123);
        CalendarTimeState weekday = new(new DateOnly(2026, 9, 8), 12);
        CalendarTimeState weekend = new(new DateOnly(2026, 9, 12), 12);

        int rainyWeekday = generator.GenerateHour(weekdayState, weekday, new WorldDayContext(weekday with { Hour = 0 }, "rain", 14, 0.5m), profile).Count;
        int sunnyWeekend = generator.GenerateHour(weekendState, weekend, new WorldDayContext(weekend with { Hour = 0 }, "sunny", 27, 1.2m), profile).Count;

        Assert.True(sunnyWeekend > rainyWeekday);
    }

    private static WorldGenerationProfile CreateProfile(IReadOnlyList<SegmentGenerationProfile>? segments = null) =>
        new(
            "park",
            FootTrafficMultiplier: 1,
            WeekdayMultiplier: 1,
            WeekendMultiplier: 1.25m,
            HourlyOpportunities: Repeat(12),
            Weather:
            [
                new WeatherGenerationProfile("sunny", 0.7m, 1.2m, 18, 34),
                new WeatherGenerationProfile("rain", 0.3m, 0.5m, 8, 20),
            ],
            Segments: segments ?? [new SegmentGenerationProfile("student", Repeat(1))]);

    private static decimal[] Repeat(decimal value) => Enumerable.Repeat(value, 24).ToArray();
}

using Zest.Config;
using Zest.Domain.World;
using Xunit;

namespace Zest.Config.Tests;

public sealed class WorldGenerationProfileFactoryTests
{
    [Fact]
    public void Park_weather_traffic_and_student_mix_are_loaded_from_config()
    {
        ContentConfig config = new ConfigLoader().Load(FindConfigRoot());

        WorldGenerationProfile profile = WorldGenerationProfileFactory.Create(config, "park");

        Assert.Equal("park", profile.LocationId);
        Assert.Equal(24, profile.HourlyOpportunities.Count);
        Assert.Contains(profile.Weather, item => item.Id == "sunny");
        Assert.Contains(profile.Weather, item => item.Id == "cloudy");
        Assert.Contains(profile.Weather, item => item.Id == "rain");
        Assert.Contains(profile.Segments, item => item.Id == "student");
    }

    private static string FindConfigRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "config", "manifest.json");
            if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find repository config directory.");
    }
}

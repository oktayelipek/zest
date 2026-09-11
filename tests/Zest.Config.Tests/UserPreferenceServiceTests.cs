using Zest.Persistence;
using Xunit;

namespace Zest.Config.Tests;

public sealed class UserPreferenceServiceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "zest-preferences-" + Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public void Preferences_round_trip_with_normalized_text_scale()
    {
        UserPreferenceService service = new();
        service.Save(_path, new UserPreferences(1, 9f, true));
        UserPreferences loaded = service.Load(_path);
        Assert.Equal(1.35f, loaded.TextScale);
        Assert.True(loaded.ReducedMotion);
        Assert.True(loaded.SoundEnabled);
    }

    [Fact]
    public void Missing_or_future_preferences_fall_back_to_safe_defaults()
    {
        UserPreferenceService service = new();
        Assert.Equal(UserPreferences.Default, service.Load(_path));
        File.WriteAllText(_path, "{\"version\":99,\"textScale\":1.35,\"reducedMotion\":true}");
        Assert.Equal(UserPreferences.Default, service.Load(_path));
    }

    [Fact]
    public void Corrupt_preferences_fall_back_to_safe_defaults()
    {
        UserPreferenceService service = new();
        File.WriteAllText(_path, "not-json");
        Assert.Equal(UserPreferences.Default, service.Load(_path));
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}

using Zest.Config;
using Zest.Domain;
using Xunit;

namespace Zest.Config.Tests;

public sealed class ConfigLoaderTests
{
    private static readonly string ConfigRoot = FindConfigRoot();

    [Fact]
    public void Baseline_content_loads_from_config()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot);

        Assert.True(config.Locations.ContainsKey("park"));
        Assert.True(config.Segments.ContainsKey("student"));
        Assert.True(config.Recipes.ContainsKey("classic"));
    }

    [Fact]
    public void Config_version_is_captured_in_initial_simulation_state()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot);

        GameState state = ConfigGameStateFactory.CreateEmpty(42, config);

        Assert.Equal("zest.vs0.1", state.ConfigVersion);
    }

    [Fact]
    public void Missing_recipe_reference_fails_validation()
    {
        string source = File.ReadAllText(Path.Combine(ConfigRoot, "products", "recipes.json"));
        string broken = source.Replace("\"lemon\"", "\"missing-ingredient\"", StringComparison.Ordinal);
        string tempRoot = CopyConfigToTemp();

        try
        {
            File.WriteAllText(Path.Combine(tempRoot, "products", "recipes.json"), broken);

            ConfigValidationException exception = Assert.Throws<ConfigValidationException>(
                () => new ConfigLoader().Load(tempRoot));

            Assert.Contains(exception.Errors, error => error.Contains("missing ingredient 'missing-ingredient'", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
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

    private static string CopyConfigToTemp()
    {
        string target = Path.Combine(Path.GetTempPath(), "zest-config-tests", Guid.NewGuid().ToString("N"));
        foreach (string sourceFile in Directory.EnumerateFiles(ConfigRoot, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(ConfigRoot, sourceFile);
            string targetFile = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile);
        }

        return target;
    }
}

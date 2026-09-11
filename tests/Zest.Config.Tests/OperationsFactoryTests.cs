using Zest.Config;
using Zest.Domain;
using Zest.Domain.Operations;
using Zest.Domain.Products;
using Xunit;

namespace Zest.Config.Tests;

public sealed class OperationsFactoryTests
{
    [Fact]
    public void Baseline_config_builds_recipe_work_and_operational_resources()
    {
        ContentConfig config = new ConfigLoader().Load(FindConfigRoot());
        OperationsState state = new();
        OrderScheduler scheduler = OperationsFactory.CreateScheduler(config, state);
        RecipeVersion recipe = ProductCatalogFactory.CreateRecipeBook(config).Get(new RecipeVersionId("classic", 1));

        OrderState order = scheduler.AcceptOrder(Guid.NewGuid(), recipe, 0);

        Assert.Single(recipe.WorkSteps);
        Assert.Equal("prepare", recipe.WorkSteps[0].Id);
        Assert.Equal(OrderStatus.InProgress, order.Status);
        Assert.Equal(3, state.Resources.Count);
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

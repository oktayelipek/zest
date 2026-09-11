using Zest.Config;
using Zest.Domain;
using Zest.Domain.Products;
using Xunit;

namespace Zest.Config.Tests;

public sealed class ProductCatalogFactoryTests
{
    [Fact]
    public void Classic_recipe_and_initial_inventory_are_built_from_config()
    {
        ContentConfig config = new ConfigLoader().Load(FindConfigRoot());
        RecipeBook book = ProductCatalogFactory.CreateRecipeBook(config);
        InventoryState inventory = new();
        ProductCatalogFactory.SeedInventory(config, inventory);

        Assert.True(book.Versions.ContainsKey(new RecipeVersionId("classic", 1)));
        Assert.Equal(40, inventory.Quantities["lemon"]);
    }

    private static string FindConfigRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) { string path = Path.Combine(directory.FullName, "config", "manifest.json"); if (File.Exists(path)) return Path.GetDirectoryName(path)!; directory = directory.Parent; }
        throw new DirectoryNotFoundException();
    }
}

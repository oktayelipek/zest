using Zest.Domain.Inventory;
using Zest.Domain.Products;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class ProductInventoryTests
{
    private static readonly IReadOnlyDictionary<string, IngredientProfile> Ingredients = new Dictionary<string, IngredientProfile>
    {
        ["lemon"] = new("lemon", "each", 35, .05m, 1, .9m, 0),
        ["sugar"] = new("sugar", "g", 1, 1, 0, .4m, 0),
        ["water"] = new("water", "ml", 0, 0, 0, 0, .3m),
    };

    [Fact]
    public void Editing_recipe_creates_new_immutable_version_and_history_keeps_old_version()
    {
        RecipeBook book = new(Ingredients);
        RecipeVersion first = book.CreateVersion(Draft(25));
        HistoricalOrder historical = new("order-1", first.Id, 10);
        RecipeVersion second = book.CreateVersion(Draft(35));

        Assert.Equal(new RecipeVersionId("classic", 1), first.Id);
        Assert.Equal(new RecipeVersionId("classic", 2), second.Id);
        Assert.Equal(first.Id, historical.RecipeVersionId);
        Assert.Equal(25, first.Components.Single(item => item.IngredientId == "sugar").Quantity);
        Assert.Equal(35, second.Components.Single(item => item.IngredientId == "sugar").Quantity);
    }

    [Fact]
    public void Product_cogs_and_workload_are_derived_at_version_creation()
    {
        RecipeVersion recipe = new RecipeBook(Ingredients).CreateVersion(Draft(25));

        Assert.Equal(60, recipe.CogsMinor);
        Assert.Equal(30, recipe.WorkloadSeconds);
        Assert.True(recipe.Product.Sweetness > 0);
        Assert.True(recipe.Product.Acidity > 0);
    }

    [Fact]
    public void Accepted_orders_cannot_oversell()
    {
        RecipeVersion recipe = new RecipeBook(Ingredients).CreateVersion(Draft(25));
        InventoryState state = new();
        InventoryService inventory = new(state);
        inventory.ReceiveLot("lemons", "lemon", 1);
        inventory.ReceiveLot("sugar", "sugar", 25);
        inventory.ReceiveLot("water", "water", 300);

        Assert.True(inventory.TryReserve("order-1", recipe));
        Assert.False(inventory.TryReserve("order-2", recipe));
        Assert.Equal(0, inventory.Available("lemon"));
    }

    [Fact]
    public void Serve_consumes_once_and_cancel_releases_once()
    {
        RecipeVersion recipe = new RecipeBook(Ingredients).CreateVersion(Draft(25));
        InventoryState state = new();
        InventoryService inventory = Seed(state);

        Assert.True(inventory.TryReserve("served", recipe));
        Assert.True(inventory.Consume("served"));
        Assert.False(inventory.Consume("served"));
        Assert.False(inventory.Release("served"));
        Assert.Equal(1, state.Quantities["lemon"]);

        Assert.True(inventory.TryReserve("cancelled", recipe));
        Assert.True(inventory.Release("cancelled"));
        Assert.False(inventory.Release("cancelled"));
        Assert.False(inventory.Consume("cancelled"));
        Assert.Equal(1, inventory.Available("lemon"));
    }

    [Fact]
    public void Inventory_never_goes_negative_across_many_order_sequences()
    {
        RecipeVersion recipe = new RecipeBook(Ingredients).CreateVersion(Draft(25));
        for (int seed = 0; seed < 1_000; seed++)
        {
            InventoryState state = new(); InventoryService inventory = Seed(state);
            for (int order = 0; order < 10; order++)
            {
                string id = $"{seed}-{order}";
                if (!inventory.TryReserve(id, recipe)) continue;
                if ((seed + order) % 2 == 0) inventory.Consume(id); else inventory.Release(id);
            }
            Assert.All(state.Lots.Values, lot => Assert.True(lot.RemainingQuantity >= 0));
        }
    }

    private static RecipeDraft Draft(decimal sugar) => new("classic", [new("lemon", 1, "each"), new("sugar", sugar, "g"), new("water", 300, "ml")], 350, 30);
    private static InventoryService Seed(InventoryState state) { InventoryService service = new(state); service.ReceiveLot("lemons", "lemon", 2); service.ReceiveLot("sugar", "sugar", 50); service.ReceiveLot("water", "water", 600); return service; }
}

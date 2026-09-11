using Zest.Domain;
using Zest.Domain.Inventory;
using Zest.Domain.Products;

namespace Zest.Config;

public static class ProductCatalogFactory
{
    public static RecipeBook CreateRecipeBook(ContentConfig config)
    {
        Dictionary<string, IngredientProfile> ingredients = config.Ingredients.Values.ToDictionary(
            item => item.Id,
            item => new IngredientProfile(item.Id, item.Unit, item.UnitCostMinor, item.Sweetness, item.Acidity, item.Intensity, item.Coldness),
            StringComparer.Ordinal);
        RecipeBook book = new(ingredients);
        foreach (RecipeDefinition recipe in config.Recipes.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            book.CreateVersion(new RecipeDraft(
                recipe.Id,
                recipe.Ingredients.Select(item => new RecipeComponent(item.IngredientId, item.Quantity, item.Unit)).ToArray(),
                recipe.SalePriceMinor,
                recipe.PrepSeconds,
                recipe.Steps.Select(step => new RecipeWorkStep(step.Id, step.DurationSeconds, step.StationId, step.EquipmentId, step.StaffId)).ToArray()));
        }
        return book;
    }

    public static void SeedInventory(ContentConfig config, InventoryState state)
    {
        InventoryService inventory = new(state);
        foreach (IngredientDefinition ingredient in config.Ingredients.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            if (ingredient.InitialStock > 0) inventory.ReceiveLot($"initial-{ingredient.Id}", ingredient.Id, ingredient.InitialStock);
    }
}

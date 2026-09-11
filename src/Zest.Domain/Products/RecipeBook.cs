using System.Collections.ObjectModel;

namespace Zest.Domain.Products;

public sealed class RecipeBook
{
    private readonly Dictionary<RecipeVersionId, RecipeVersion> _versions = [];
    private readonly IReadOnlyDictionary<string, IngredientProfile> _ingredients;
    public RecipeBook(IReadOnlyDictionary<string, IngredientProfile> ingredients) => _ingredients = ingredients;
    public IReadOnlyDictionary<RecipeVersionId, RecipeVersion> Versions => new ReadOnlyDictionary<RecipeVersionId, RecipeVersion>(_versions);
    public RecipeVersion CreateVersion(RecipeDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        int number = _versions.Keys.Where(key => key.RecipeId == draft.RecipeId).Select(key => key.Version).DefaultIfEmpty().Max() + 1;
        RecipeVersion version = Derive(draft, new RecipeVersionId(draft.RecipeId, number));
        _versions.Add(version.Id, version);
        return version;
    }
    public RecipeVersion Get(RecipeVersionId id) => _versions.TryGetValue(id, out RecipeVersion? value) ? value : throw new KeyNotFoundException($"Unknown recipe version '{id}'.");
    private RecipeVersion Derive(RecipeDraft draft, RecipeVersionId id)
    {
        if (draft.Components.Count == 0 || draft.WorkloadSeconds <= 0 || draft.SalePriceMinor <= 0) throw new ArgumentException("Recipe values must be positive.", nameof(draft));
        decimal total = draft.Components.Sum(item => item.Quantity);
        if (total <= 0) throw new ArgumentException("Recipe quantity must be positive.", nameof(draft));
        decimal sweet = 0, acid = 0, intensity = 0, cold = 0, cogs = 0;
        foreach (RecipeComponent component in draft.Components)
        {
            if (!_ingredients.TryGetValue(component.IngredientId, out IngredientProfile? ingredient)) throw new KeyNotFoundException($"Unknown ingredient '{component.IngredientId}'.");
            if (component.Quantity <= 0 || component.Unit != ingredient.Unit) throw new ArgumentException($"Invalid quantity/unit for '{component.IngredientId}'.", nameof(draft));
            sweet += ingredient.Sweetness * component.Quantity; acid += ingredient.Acidity * component.Quantity;
            intensity += ingredient.Intensity * component.Quantity; cold += ingredient.Coldness * component.Quantity;
            cogs += ingredient.UnitCostMinor * component.Quantity;
        }
        return new RecipeVersion(id, Array.AsReadOnly(draft.Components.ToArray()), draft.SalePriceMinor, new(sweet / total, acid / total, intensity / total, cold / total), (long)decimal.Ceiling(cogs), draft.WorkloadSeconds)
        {
            WorkSteps = Array.AsReadOnly((draft.WorkSteps ?? Array.Empty<RecipeWorkStep>()).ToArray()),
        };
    }
}

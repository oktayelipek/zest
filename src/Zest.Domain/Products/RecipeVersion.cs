namespace Zest.Domain.Products;

public readonly record struct RecipeVersionId(string RecipeId, int Version) { public override string ToString() => $"{RecipeId}@{Version}"; }
public sealed record RecipeComponent(string IngredientId, decimal Quantity, string Unit);
public sealed record RecipeVersion(RecipeVersionId Id, IReadOnlyList<RecipeComponent> Components, long SalePriceMinor, ProductProfile Product, long CogsMinor, int WorkloadSeconds)
{
    public IReadOnlyList<RecipeWorkStep> WorkSteps { get; init; } = Array.Empty<RecipeWorkStep>();
}
public sealed record RecipeDraft(
    string RecipeId,
    IReadOnlyList<RecipeComponent> Components,
    long SalePriceMinor,
    int WorkloadSeconds,
    IReadOnlyList<RecipeWorkStep>? WorkSteps = null);

namespace Zest.Domain.Products;

public sealed record ProductProfile(decimal Sweetness, decimal Acidity, decimal Intensity, decimal Coldness);
public sealed record IngredientProfile(string Id, string Unit, long UnitCostMinor, decimal Sweetness, decimal Acidity, decimal Intensity, decimal Coldness);

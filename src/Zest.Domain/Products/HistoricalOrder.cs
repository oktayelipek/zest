namespace Zest.Domain.Products;
public sealed record HistoricalOrder(string OrderId, RecipeVersionId RecipeVersionId, long AcceptedAtSimTime);

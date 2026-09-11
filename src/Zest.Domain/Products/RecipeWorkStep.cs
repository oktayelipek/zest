namespace Zest.Domain.Products;

public sealed record RecipeWorkStep(
    string Id,
    int DurationSeconds,
    string StationId,
    string EquipmentId,
    string StaffId);

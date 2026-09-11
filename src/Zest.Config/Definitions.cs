namespace Zest.Config;

public sealed record SegmentDefinition(
    string Id,
    string DisplayName,
    decimal ArrivalWeight,
    decimal PriceSensitivity,
    IReadOnlyList<HourlyWeightDefinition> HourlyWeights,
    long ReferencePriceMinor,
    decimal NoticeProbability,
    decimal InterestProbability,
    decimal OutsideOptionUtility,
    IReadOnlyDictionary<string, decimal> TasteWeights);

public sealed record HourlyWeightDefinition(int Hour, decimal Weight);

public sealed record IngredientDefinition(
    string Id,
    string DisplayName,
    string Unit,
    decimal InitialStock,
    long UnitCostMinor,
    decimal Sweetness,
    decimal Acidity,
    decimal Intensity,
    decimal Coldness);

public sealed record RecipeIngredientDefinition(
    string IngredientId,
    decimal Quantity,
    string Unit);

public sealed record RecipeDefinition(
    string Id,
    string DisplayName,
    IReadOnlyList<RecipeIngredientDefinition> Ingredients,
    long SalePriceMinor,
    int PrepSeconds,
    IReadOnlyList<RecipeStepDefinition> Steps);

public sealed record RecipeStepDefinition(
    string Id,
    int DurationSeconds,
    string StationId,
    string EquipmentId,
    string StaffId);

public sealed record LocationDefinition(
    string Id,
    string DisplayName,
    decimal FootTrafficMultiplier,
    decimal WeekdayMultiplier,
    decimal WeekendMultiplier,
    IReadOnlyList<HourlyTrafficDefinition> TrafficCurve);

public sealed record HourlyTrafficDefinition(int Hour, decimal OpportunitiesPerHour);

public sealed record WeatherDefinition(
    string Id,
    string DisplayName,
    decimal ArrivalMultiplier,
    decimal Weight,
    decimal MinTemperatureC,
    decimal MaxTemperatureC);

public sealed record EquipmentDefinition(
    string Id,
    string DisplayName,
    int Capacity,
    int CycleSeconds);

public sealed record StationDefinition(
    string Id,
    string DisplayName,
    int Capacity);

public sealed record StaffDefinition(
    string Id,
    string DisplayName,
    decimal ProductivityMultiplier,
    long HourlyCostMinor);

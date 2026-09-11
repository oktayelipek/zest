namespace Zest.Config;

public sealed record ContentConfig(
    string Version,
    IReadOnlyDictionary<string, SegmentDefinition> Segments,
    IReadOnlyDictionary<string, IngredientDefinition> Ingredients,
    IReadOnlyDictionary<string, RecipeDefinition> Recipes,
    IReadOnlyDictionary<string, LocationDefinition> Locations,
    IReadOnlyDictionary<string, WeatherDefinition> Weather,
    IReadOnlyDictionary<string, StationDefinition> Stations,
    IReadOnlyDictionary<string, EquipmentDefinition> Equipment,
    IReadOnlyDictionary<string, StaffDefinition> Staff);

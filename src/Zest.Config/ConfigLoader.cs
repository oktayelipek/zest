using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Zest.Config;

public sealed partial class ConfigLoader
{
    private static readonly HashSet<string> SupportedUnits = new(StringComparer.Ordinal)
    {
        "each", "g", "kg", "ml", "l",
    };

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public ContentConfig Load(string configRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configRoot);
        string root = Path.GetFullPath(configRoot);
        ConfigManifest manifest = Read<ConfigManifest>(Path.Combine(root, "manifest.json"));

        List<SegmentDefinition> segments = Read<List<SegmentDefinition>>(Resolve(root, manifest.Files.Segments));
        List<IngredientDefinition> ingredients = Read<List<IngredientDefinition>>(Resolve(root, manifest.Files.Ingredients));
        List<RecipeDefinition> recipes = Read<List<RecipeDefinition>>(Resolve(root, manifest.Files.Recipes));
        List<LocationDefinition> locations = Read<List<LocationDefinition>>(Resolve(root, manifest.Files.Locations));
        List<WeatherDefinition> weather = Read<List<WeatherDefinition>>(Resolve(root, manifest.Files.Weather));
        List<StationDefinition> stations = Read<List<StationDefinition>>(Resolve(root, manifest.Files.Stations));
        List<EquipmentDefinition> equipment = Read<List<EquipmentDefinition>>(Resolve(root, manifest.Files.Equipment));
        List<StaffDefinition> staff = Read<List<StaffDefinition>>(Resolve(root, manifest.Files.Staff));

        List<string> errors = Validate(manifest, segments, ingredients, recipes, locations, weather, stations, equipment, staff);
        if (errors.Count > 0)
        {
            throw new ConfigValidationException(errors);
        }

        return new ContentConfig(
            manifest.Version,
            Index(segments, item => item.Id),
            Index(ingredients, item => item.Id),
            Index(recipes, item => item.Id),
            Index(locations, item => item.Id),
            Index(weather, item => item.Id),
            Index(stations, item => item.Id),
            Index(equipment, item => item.Id),
            Index(staff, item => item.Id));
    }

    private T Read<T>(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions)
                ?? throw new ConfigValidationException([$"'{path}' contains null JSON."]);
        }
        catch (ConfigValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new ConfigValidationException([$"Cannot load '{path}': {exception.Message}"]);
        }
    }

    private static string Resolve(string root, string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        string rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigValidationException([$"Config path escapes root: '{relativePath}'."]);
        }

        return fullPath;
    }

    private static List<string> Validate(
        ConfigManifest manifest,
        List<SegmentDefinition> segments,
        List<IngredientDefinition> ingredients,
        List<RecipeDefinition> recipes,
        List<LocationDefinition> locations,
        List<WeatherDefinition> weather,
        List<StationDefinition> stations,
        List<EquipmentDefinition> equipment,
        List<StaffDefinition> staff)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(manifest.Version)) errors.Add("Manifest version is required.");

        ValidateIds(segments.Select(item => item.Id), "segment", errors);
        ValidateIds(ingredients.Select(item => item.Id), "ingredient", errors);
        ValidateIds(recipes.Select(item => item.Id), "recipe", errors);
        ValidateIds(locations.Select(item => item.Id), "location", errors);
        ValidateIds(weather.Select(item => item.Id), "weather", errors);
        ValidateIds(stations.Select(item => item.Id), "station", errors);
        ValidateIds(equipment.Select(item => item.Id), "equipment", errors);
        ValidateIds(staff.Select(item => item.Id), "staff", errors);

        foreach (SegmentDefinition item in segments)
        {
            Positive(item.ArrivalWeight, $"Segment '{item.Id}' arrivalWeight", errors);
            Range(item.PriceSensitivity, 0, 1, $"Segment '{item.Id}' priceSensitivity", errors);
            ValidateHourlyWeights(item.Id, item.HourlyWeights, errors);
            Positive(item.ReferencePriceMinor, $"Segment '{item.Id}' referencePriceMinor", errors);
            Range(item.NoticeProbability, 0, 1, $"Segment '{item.Id}' noticeProbability", errors);
            Range(item.InterestProbability, 0, 1, $"Segment '{item.Id}' interestProbability", errors);
            if (item.TasteWeights.Count == 0) errors.Add($"Segment '{item.Id}' must define at least one taste weight.");
        }

        Dictionary<string, IngredientDefinition> ingredientIndex = IndexAllowingDuplicates(ingredients, item => item.Id);
        foreach (IngredientDefinition item in ingredients)
        {
            Positive(item.InitialStock, $"Ingredient '{item.Id}' initialStock", errors, allowZero: true);
            Positive(item.UnitCostMinor, $"Ingredient '{item.Id}' unitCostMinor", errors, allowZero: true);
            if (!SupportedUnits.Contains(item.Unit)) errors.Add($"Ingredient '{item.Id}' has unsupported unit '{item.Unit}'.");
            Range(item.Sweetness, 0, 1, $"Ingredient '{item.Id}' sweetness", errors);
            Range(item.Acidity, 0, 1, $"Ingredient '{item.Id}' acidity", errors);
            Range(item.Intensity, 0, 1, $"Ingredient '{item.Id}' intensity", errors);
            Range(item.Coldness, 0, 1, $"Ingredient '{item.Id}' coldness", errors);
        }

        Dictionary<string, StationDefinition> stationIndex = IndexAllowingDuplicates(stations, item => item.Id);
        Dictionary<string, EquipmentDefinition> equipmentIndex = IndexAllowingDuplicates(equipment, item => item.Id);
        Dictionary<string, StaffDefinition> staffIndex = IndexAllowingDuplicates(staff, item => item.Id);
        foreach (RecipeDefinition recipe in recipes)
        {
            Positive(recipe.SalePriceMinor, $"Recipe '{recipe.Id}' salePriceMinor", errors);
            Positive(recipe.PrepSeconds, $"Recipe '{recipe.Id}' prepSeconds", errors);
            if (recipe.Ingredients.Count == 0) errors.Add($"Recipe '{recipe.Id}' must reference at least one ingredient.");
            foreach (RecipeIngredientDefinition usage in recipe.Ingredients)
            {
                Positive(usage.Quantity, $"Recipe '{recipe.Id}' ingredient '{usage.IngredientId}' quantity", errors);
                if (!ingredientIndex.TryGetValue(usage.IngredientId, out IngredientDefinition? ingredient))
                {
                    errors.Add($"Recipe '{recipe.Id}' references missing ingredient '{usage.IngredientId}'.");
                }
                else if (!string.Equals(usage.Unit, ingredient.Unit, StringComparison.Ordinal))
                {
                    errors.Add($"Recipe '{recipe.Id}' uses '{usage.IngredientId}' as '{usage.Unit}', expected '{ingredient.Unit}'.");
                }
            }
            if (recipe.Steps.Count == 0) errors.Add($"Recipe '{recipe.Id}' must define at least one work step.");
            ValidateIds(recipe.Steps.Select(step => step.Id), $"step in recipe '{recipe.Id}'", errors);
            foreach (RecipeStepDefinition step in recipe.Steps)
            {
                Positive(step.DurationSeconds, $"Recipe '{recipe.Id}' step '{step.Id}' durationSeconds", errors);
                if (!stationIndex.ContainsKey(step.StationId)) errors.Add($"Recipe '{recipe.Id}' step '{step.Id}' references missing station '{step.StationId}'.");
                if (!equipmentIndex.ContainsKey(step.EquipmentId)) errors.Add($"Recipe '{recipe.Id}' step '{step.Id}' references missing equipment '{step.EquipmentId}'.");
                if (!staffIndex.ContainsKey(step.StaffId)) errors.Add($"Recipe '{recipe.Id}' step '{step.Id}' references missing staff '{step.StaffId}'.");
            }
        }

        HashSet<string> recipeIds = recipes.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        foreach (SegmentDefinition segment in segments)
        {
            foreach (string recipeId in segment.TasteWeights.Keys)
            {
                if (!recipeIds.Contains(recipeId)) errors.Add($"Segment '{segment.Id}' references missing recipe '{recipeId}' in tasteWeights.");
            }
        }

        foreach (LocationDefinition item in locations)
        {
            Positive(item.FootTrafficMultiplier, $"Location '{item.Id}' footTrafficMultiplier", errors);
            Positive(item.WeekdayMultiplier, $"Location '{item.Id}' weekdayMultiplier", errors);
            Positive(item.WeekendMultiplier, $"Location '{item.Id}' weekendMultiplier", errors);
            if (item.TrafficCurve.Count != 24 || item.TrafficCurve.Select(point => point.Hour).Distinct().Count() != 24
                || item.TrafficCurve.Any(point => point.Hour is < 0 or > 23))
            {
                errors.Add($"Location '{item.Id}' trafficCurve must contain each hour 0-23 exactly once.");
            }
            foreach (HourlyTrafficDefinition point in item.TrafficCurve)
            {
                Positive(point.OpportunitiesPerHour, $"Location '{item.Id}' hour {point.Hour} opportunitiesPerHour", errors, allowZero: true);
            }
        }
        foreach (WeatherDefinition item in weather)
        {
            Positive(item.ArrivalMultiplier, $"Weather '{item.Id}' arrivalMultiplier", errors);
            Positive(item.Weight, $"Weather '{item.Id}' weight", errors);
            if (item.MinTemperatureC > item.MaxTemperatureC) errors.Add($"Weather '{item.Id}' minTemperatureC must not exceed maxTemperatureC.");
        }
        foreach (EquipmentDefinition item in equipment)
        {
            Positive(item.Capacity, $"Equipment '{item.Id}' capacity", errors);
            Positive(item.CycleSeconds, $"Equipment '{item.Id}' cycleSeconds", errors);
        }
        foreach (StationDefinition item in stations)
        {
            Positive(item.Capacity, $"Station '{item.Id}' capacity", errors);
        }
        foreach (StaffDefinition item in staff)
        {
            Positive(item.ProductivityMultiplier, $"Staff '{item.Id}' productivityMultiplier", errors);
            Positive(item.HourlyCostMinor, $"Staff '{item.Id}' hourlyCostMinor", errors, allowZero: true);
        }

        return errors;
    }

    private static void ValidateHourlyWeights(string segmentId, IReadOnlyList<HourlyWeightDefinition> weights, List<string> errors)
    {
        if (weights.Count != 24 || weights.Select(point => point.Hour).Distinct().Count() != 24
            || weights.Any(point => point.Hour is < 0 or > 23))
        {
            errors.Add($"Segment '{segmentId}' hourlyWeights must contain each hour 0-23 exactly once.");
        }
        foreach (HourlyWeightDefinition point in weights)
        {
            Positive(point.Weight, $"Segment '{segmentId}' hour {point.Hour} weight", errors, allowZero: true);
        }
    }

    private static void ValidateIds(IEnumerable<string> ids, string kind, List<string> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id) || !StableIdPattern().IsMatch(id)) errors.Add($"Invalid stable {kind} ID '{id}'.");
            else if (!seen.Add(id)) errors.Add($"Duplicate {kind} ID '{id}'.");
        }
    }

    private static void Positive(decimal value, string name, List<string> errors, bool allowZero = false)
    {
        if (allowZero ? value < 0 : value <= 0) errors.Add($"{name} must be {(allowZero ? "non-negative" : "positive")}.");
    }

    private static void Range(decimal value, decimal min, decimal max, string name, List<string> errors)
    {
        if (value < min || value > max) errors.Add($"{name} must be in range [{min}, {max}].");
    }

    private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> id) =>
        new Dictionary<string, T>(items.ToDictionary(id, StringComparer.Ordinal), StringComparer.Ordinal);

    private static Dictionary<string, T> IndexAllowingDuplicates<T>(IEnumerable<T> items, Func<T, string> id)
    {
        Dictionary<string, T> result = new(StringComparer.Ordinal);
        foreach (T item in items) result.TryAdd(id(item), item);
        return result;
    }

    [GeneratedRegex("^[a-z][a-z0-9.-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();
}

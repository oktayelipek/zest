using Zest.Domain;
using Zest.Domain.DayCycle;
using Zest.Domain.Economy;
using Zest.Domain.Operations;

namespace Zest.Config;

/// <summary>Builds the one configured session used by every host.</summary>
public static class ConfigLiveSessionFactory
{
    public static LiveDayRunner Create(GameState state, ContentConfig config, DateOnly firstDate)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(config);
        ProductCatalogFactory.SeedInventory(config, state.Inventory);
        LiveDayRunner runner = CreateRunner(state, config, firstDate);
        new EconomyService(state.Business).InitializeOpeningCash(new Money(10_000));
        return runner;
    }

    public static LiveDayRunner CreateRunner(GameState state, ContentConfig config, DateOnly firstDate)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(config);
        LiveDayRunner runner = new(state, ProductCatalogFactory.CreateRecipeBook(config),
            CustomerGenerationProfileFactory.Create(config), WorldGenerationProfileFactory.Create(config, "park"), firstDate);
        foreach (StationDefinition station in config.Stations.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            runner.RegisterResource(ResourceKind.Station, station.Id, station.Capacity);
        foreach (EquipmentDefinition equipment in config.Equipment.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            runner.RegisterResource(ResourceKind.Equipment, equipment.Id, equipment.Capacity);
        foreach (StaffDefinition staff in config.Staff.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            runner.RegisterResource(ResourceKind.Staff, staff.Id);
        return runner;
    }
}

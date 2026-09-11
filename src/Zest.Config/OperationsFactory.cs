using Zest.Domain;
using Zest.Domain.Operations;

namespace Zest.Config;

public static class OperationsFactory
{
    public static OrderScheduler CreateScheduler(ContentConfig config, OperationsState state)
    {
        ArgumentNullException.ThrowIfNull(config);
        OrderScheduler scheduler = new(state);
        foreach (StationDefinition station in config.Stations.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            scheduler.RegisterResource(ResourceKind.Station, station.Id, station.Capacity);
        foreach (EquipmentDefinition equipment in config.Equipment.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            scheduler.RegisterResource(ResourceKind.Equipment, equipment.Id, equipment.Capacity);
        foreach (StaffDefinition staff in config.Staff.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
            scheduler.RegisterResource(ResourceKind.Staff, staff.Id);
        return scheduler;
    }
}

namespace Zest.Domain.World;

public sealed record WorldDayContext(
    CalendarTimeState Time,
    string WeatherId,
    decimal TemperatureC,
    decimal TrafficMultiplier);

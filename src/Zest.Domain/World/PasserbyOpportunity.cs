namespace Zest.Domain.World;

/// <summary>A potential customer encounter; it intentionally contains no sale outcome.</summary>
public sealed record PasserbyOpportunity(
    string Id,
    CalendarTimeState Time,
    string LocationId,
    string SegmentId,
    string WeatherId,
    decimal TemperatureC);

namespace Zest.Domain.World;

public readonly record struct CalendarTimeState(DateOnly Date, int Hour)
{
    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    public bool IsWeekend => DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}

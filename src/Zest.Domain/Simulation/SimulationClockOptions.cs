namespace Zest.Domain.Simulation;

public sealed record SimulationClockOptions
{
    public static SimulationClockOptions Default { get; } = new();

    /// <summary>Duration represented by one authoritative simulation step.
    /// At 1× a 10-hour business day (36 000 SimTime units) takes ~6 real minutes.</summary>
    public TimeSpan FixedStep { get; init; } = TimeSpan.FromMilliseconds(10);

    /// <summary>Number of fixed simulation steps in one game day.</summary>
    public long StepsPerDay { get; init; } = 86_400;

    internal void Validate()
    {
        if (FixedStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(FixedStep), "Fixed step must be positive.");
        }

        if (StepsPerDay <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StepsPerDay), "Steps per day must be positive.");
        }
    }
}


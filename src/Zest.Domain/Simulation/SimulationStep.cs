namespace Zest.Domain.Simulation;

public readonly record struct SimulationStep(
    long Sequence,
    int DayIndex,
    long StepWithinDay,
    bool EndsDay,
    bool StartsDay);


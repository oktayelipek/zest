using Zest.Domain.Simulation;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class SimulationClockTests
{
    private static readonly SimulationClockOptions TestOptions = new()
    {
        FixedStep = TimeSpan.FromMilliseconds(100),
        StepsPerDay = 10,
    };

    [Fact]
    public void Frame_partitioning_does_not_change_the_result()
    {
        GameState highFpsState = GameStateFactory.CreateEmpty(42);
        SimulationClock highFpsClock = new(highFpsState, TestOptions);
        List<long> highFpsEvents = new();

        for (int frame = 0; frame < 100; frame++)
        {
            highFpsClock.Advance(TimeSpan.FromMilliseconds(10), (_, step) => highFpsEvents.Add(step.Sequence));
        }

        GameState lowFpsState = GameStateFactory.CreateEmpty(42);
        SimulationClock lowFpsClock = new(lowFpsState, TestOptions);
        List<long> lowFpsEvents = new();

        for (int frame = 0; frame < 10; frame++)
        {
            lowFpsClock.Advance(TimeSpan.FromMilliseconds(100), (_, step) => lowFpsEvents.Add(step.Sequence));
        }

        Assert.Equal(highFpsState.SimTime, lowFpsState.SimTime);
        Assert.Equal(highFpsState.DayIndex, lowFpsState.DayIndex);
        Assert.Equal(highFpsEvents, lowFpsEvents);
    }

    [Fact]
    public void Pause_does_not_advance_time_or_invoke_business_steps()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        SimulationClock clock = new(state, TestOptions);
        int businessStepCount = 0;
        clock.SetSpeed(SimulationSpeed.Paused);

        int executed = clock.Advance(TimeSpan.FromHours(1), (_, _) => businessStepCount++);

        Assert.Equal(0, executed);
        Assert.Equal(0, state.SimTime);
        Assert.Equal(0, state.Business.CashMinorUnits);
        Assert.Equal(0, businessStepCount);
    }

    [Fact]
    public void Speed_changes_preserve_event_ordering()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        SimulationClock clock = new(state, TestOptions);
        List<long> events = new();

        clock.Advance(TimeSpan.FromMilliseconds(100), (_, step) => events.Add(step.Sequence));
        clock.SetSpeed(SimulationSpeed.Quadruple);
        clock.Advance(TimeSpan.FromMilliseconds(100), (_, step) => events.Add(step.Sequence));
        clock.SetSpeed(SimulationSpeed.Double);
        clock.Advance(TimeSpan.FromMilliseconds(100), (_, step) => events.Add(step.Sequence));

        Assert.Equal([1L, 2L, 3L, 4L, 5L, 6L, 7L], events);
        Assert.Equal(7, state.SimTime);
    }

    [Fact]
    public void Day_boundary_advances_authoritative_day_state()
    {
        GameState state = GameStateFactory.CreateEmpty(42);
        SimulationClock clock = new(state, TestOptions);
        SimulationStep boundary = default;

        clock.Advance(TimeSpan.FromSeconds(1), (_, step) => boundary = step);

        Assert.Equal(10, state.SimTime);
        Assert.Equal(1, state.DayIndex);
        Assert.Equal(1, state.World.ParkDayIndex);
        Assert.True(boundary.EndsDay);
        Assert.True(boundary.StartsDay);
        Assert.Equal(0, boundary.StepWithinDay);
    }

    [Fact]
    public void Invalid_speed_is_rejected()
    {
        SimulationClock clock = new(GameStateFactory.CreateEmpty(42), TestOptions);

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed((SimulationSpeed)3));
    }
}


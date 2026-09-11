namespace Zest.Domain.Simulation;

/// <summary>
/// Advances authoritative simulation time in deterministic fixed steps.
/// Render frames contribute elapsed time but never become simulation steps themselves.
/// </summary>
public sealed class SimulationClock
{
    private readonly GameState _state;
    private readonly SimulationClockOptions _options;
    private long _scaledRealTimeRemainder;

    public SimulationClock(GameState state, SimulationClockOptions? options = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _options = options ?? SimulationClockOptions.Default;
        _options.Validate();
    }

    public SimulationSpeed Speed { get; private set; } = SimulationSpeed.Normal;

    public TimeSpan FixedStep => _options.FixedStep;

    public void SetSpeed(SimulationSpeed speed)
    {
        if (speed is not SimulationSpeed.Paused
            and not SimulationSpeed.Normal
            and not SimulationSpeed.Double
            and not SimulationSpeed.Quadruple)
        {
            throw new ArgumentOutOfRangeException(nameof(speed));
        }

        Speed = speed;
    }

    /// <summary>
    /// Adds elapsed wall-clock time and invokes <paramref name="onStep"/> once per
    /// completed authoritative step, in sequence order.
    /// </summary>
    /// <returns>The number of simulation steps executed.</returns>
    public int Advance(TimeSpan elapsed, Action<GameState, SimulationStep>? onStep = null)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        if (Speed == SimulationSpeed.Paused || elapsed == TimeSpan.Zero)
        {
            return 0;
        }

        checked
        {
            _scaledRealTimeRemainder += elapsed.Ticks * (int)Speed;
        }

        int executedSteps = 0;
        long fixedStepTicks = _options.FixedStep.Ticks;

        while (_scaledRealTimeRemainder >= fixedStepTicks)
        {
            _scaledRealTimeRemainder -= fixedStepTicks;
            ExecuteStep(onStep);
            executedSteps = checked(executedSteps + 1);
        }

        return executedSteps;
    }

    private void ExecuteStep(Action<GameState, SimulationStep>? onStep)
    {
        long nextSequence = checked(_state.SimTime + 1);
        long stepWithinDay = nextSequence % _options.StepsPerDay;
        bool endsDay = stepWithinDay == 0;

        _state.SimTime = nextSequence;
        if (endsDay)
        {
            _state.DayIndex = checked(_state.DayIndex + 1);
            _state.World.ParkDayIndex = _state.DayIndex;
        }

        onStep?.Invoke(
            _state,
            new SimulationStep(
                nextSequence,
                _state.DayIndex,
                stepWithinDay,
                EndsDay: endsDay,
                StartsDay: endsDay));
    }
}


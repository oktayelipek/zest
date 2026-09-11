using Godot;

namespace Zest.Game;

public enum VendorPose
{
    Idle,
    Prepare,
    Pour,
    Handoff,
}

/// <summary>
/// Presentation-only vendor pose selector. Simulation code owns the service state;
/// this node only maps that state to an approved visual key pose.
/// </summary>
public partial class VendorVisual : AnimatedSprite2D
{
    private bool _initialized;
    private Vector2 _restPosition;
    private double _motionTime;

    public VendorPose Pose { get; private set; }

    public override void _Ready()
    {
        _restPosition = Position;
        SetPose(VendorPose.Idle);
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _motionTime += delta;
        // Whole logical-pixel movement keeps the 3x world render crisp.
        int phase = (int)(_motionTime / .14) % 10;
        float offset = Pose == VendorPose.Idle && phase is 2 or 3 ? -1 : 0;
        Position = _restPosition + Vector2.Down * offset;
    }

    public void SetPose(VendorPose pose)
    {
        StringName animation = pose switch
        {
            VendorPose.Prepare => "prepare",
            VendorPose.Pour => "pour",
            VendorPose.Handoff => "handoff",
            _ => "idle",
        };
        if (_initialized && Pose == pose && Animation == animation) return;
        Pose = pose;
        _initialized = true;
        _motionTime = 0;
        Position = _restPosition;
        Play(animation);
    }
}

using Godot;

namespace Zest.Game;

public partial class LayeredStandPreview : Node2D
{
    private int _frames;
    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Linear;
        var background = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://art/production/final-grid-v02/bg_riverside_640x360_v02.png"),
            Centered = true,
            ZIndex = -10,
        };
        AddChild(background);
        var stand = GD.Load<PackedScene>("res://scenes/zest_stand_body.tscn").Instantiate<Node2D>();
        stand.Position = new Vector2(0, 30);
        AddChild(stand);
        var camera = new Camera2D { Enabled = true, Zoom = Vector2.One * 3 };
        AddChild(camera);
    }

    public override void _Process(double delta)
    {
        if (!OS.GetCmdlineUserArgs().Contains("--capture")) return;
        if (++_frames != 10) return;
        string path = ProjectSettings.GlobalizePath("res://../docs/artifacts/p0-layered-stand-vendor-1080p.png");
        Error result = GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"Layered stand preview: {result} -> {path}");
        GetTree().Quit(result == Error.Ok ? 0 : 1);
    }
}

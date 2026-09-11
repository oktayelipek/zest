using Godot;

namespace Zest.Game;

/// <summary>HUD-free quality gate. Tab switches between the locked target and the first HD asset proof.</summary>
public partial class Art12VisualBenchmark : Control
{
    private Control _targetView = null!;
    private Control _standView = null!;
    private Label _modeLabel = null!;
    private bool _showStand;
    private int _frames;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        BuildTargetView();
        BuildStandView();
        BuildChrome();

        _showStand = OS.GetEnvironment("ZEST_BENCHMARK_VIEW").Equals("stand", StringComparison.OrdinalIgnoreCase);
        ApplyMode();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.Tab) return;
        _showStand = !_showStand;
        ApplyMode();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!OS.GetEnvironment("ZEST_CAPTURE_BENCHMARK").Equals("1", StringComparison.Ordinal)) return;
        _frames++;
        if (_frames < 12) return;
        Image? image = GetViewport().GetTexture().GetImage();
        if (image is null) { GetTree().Quit(1); return; }
        string name = _showStand ? "art-12-stand-benchmark.png" : "art-12-visual-benchmark.png";
        Error result = image.SavePng(ProjectSettings.GlobalizePath($"res://../docs/artifacts/{name}"));
        GetTree().Quit(result == Error.Ok ? 0 : 1);
    }

    private void BuildTargetView()
    {
        _targetView = new Control { Name = "LockedWorldTarget" };
        _targetView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_targetView);

        TextureRect target = new()
        {
            Texture = ResourceLoader.Load<Texture2D>("res://art/benchmark/art12_world_target_v01.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        target.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _targetView.AddChild(target);
    }

    private void BuildStandView()
    {
        _standView = new Control { Name = "StandQualityProof" };
        _standView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_standView);

        ColorRect grass = new() { Color = ZestStyle.Palette.WorldGround, MouseFilter = MouseFilterEnum.Ignore };
        grass.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _standView.AddChild(grass);

        ColorRect river = new() { Color = ZestStyle.Palette.River, MouseFilter = MouseFilterEnum.Ignore };
        river.SetAnchorsPreset(LayoutPreset.TopWide);
        river.OffsetBottom = 148;
        _standView.AddChild(river);

        ColorRect bank = new() { Color = ZestStyle.Palette.WorldPath, MouseFilter = MouseFilterEnum.Ignore };
        bank.SetAnchorsPreset(LayoutPreset.BottomWide);
        bank.OffsetTop = -188;
        _standView.AddChild(bank);

        TextureRect stand = new()
        {
            Name = "HdStandCandidate",
            Texture = ResourceLoader.Load<Texture2D>("res://art/benchmark/prop_zest_stand_hd_candidate_v01.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        stand.SetAnchorsPreset(LayoutPreset.Center);
        stand.OffsetLeft = -390;
        stand.OffsetRight = 390;
        stand.OffsetTop = -310;
        stand.OffsetBottom = 310;
        _standView.AddChild(stand);
    }

    private void BuildChrome()
    {
        PanelContainer panel = new();
        panel.SetAnchorsPreset(LayoutPreset.TopLeft);
        panel.OffsetLeft = 24;
        panel.OffsetTop = 24;
        panel.OffsetRight = 390;
        panel.OffsetBottom = 78;
        StyleBoxFlat box = new() { BgColor = new Color(ZestStyle.Palette.Charcoal, .92f), BorderColor = ZestStyle.Palette.Cream };
        box.SetBorderWidthAll(1); box.SetCornerRadiusAll(8); box.SetContentMarginAll(12);
        panel.AddThemeStyleboxOverride("panel", box);
        _modeLabel = new Label();
        _modeLabel.AddThemeFontSizeOverride("font_size", 13);
        _modeLabel.AddThemeColorOverride("font_color", ZestStyle.Palette.Cream);
        panel.AddChild(_modeLabel);
        AddChild(panel);
    }

    private void ApplyMode()
    {
        _targetView.Visible = !_showStand;
        _standView.Visible = _showStand;
        _modeLabel.Text = _showStand ? "STAND QUALITY PROOF  ·  TAB: TARGET" : "LOCKED WORLD TARGET  ·  TAB: STAND";
    }
}

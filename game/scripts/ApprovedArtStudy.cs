using Godot;

namespace Zest.Game;

/// <summary>Non-shipping comparison of the approved concept and its world-resolution detail budget.</summary>
public partial class ApprovedArtStudy : Control
{
    private TextureRect _reference = null!;
    private TextureRect _worldOutput = null!;
    private Label _caption = null!;
    private bool _worldMode;
    private int _frames;
    private bool _capture;

    public override void _Ready()
    {
        GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var source = Image.LoadFromFile(ProjectSettings.GlobalizePath(
            "res://../art-source/concepts/art04/visual-target-v01/zest-park-visual-target.png"));
        if (source is null || source.IsEmpty())
        {
            GD.PushError("Approved art study source is missing; this developer scene is not an exported game asset.");
            GetTree().Quit(1);
            return;
        }
        var texture = ImageTexture.CreateFromImage(source);
        var background = new ColorRect { Color = new Color("#182923"), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _reference = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Linear,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_reference);

        // Deliberately samples the CONCEPT to assess lost detail, never exports a production sprite.
        var viewport = new SubViewport
        {
            Size = new Vector2I(640, 360),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        AddChild(viewport);
        viewport.AddChild(new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = texture,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            Size = new Vector2(640, 360),
        });
        _worldOutput = new TextureRect
        {
            Texture = viewport.GetTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_worldOutput);
        var panel = new PanelContainer { Position = new Vector2(16, 16) };
        AddChild(panel);
        _caption = new Label();
        _caption.AddThemeFontSizeOverride("font_size", 18);
        panel.AddChild(_caption);
        GetViewport().SizeChanged += LayoutStudy;
        _capture = OS.GetCmdlineUserArgs().Contains("--study-capture");
        LayoutStudy();
    }

    private void LayoutStudy()
    {
        Vector2 available = GetViewportRect().Size;
        _reference.Size = available;
        int scale = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(available.X / 640, available.Y / 360)));
        _worldOutput.Size = new Vector2(640, 360) * scale;
        _worldOutput.Position = ((available - _worldOutput.Size) / 2).Floor();
        _worldOutput.Visible = _worldMode;
        _reference.Visible = !_worldMode;
        _caption.Text = _worldMode
            ? $"2 · 640×360 DETAIL STUDY · {scale}×  |  1: approved reference  2: world study  Esc: close\nConcept sampling only — not final sprite art"
            : "1 · APPROVED VISUAL REFERENCE  |  1: reference  2: world study  Esc: close\nArt direction approved — not a gameplay screenshot";
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.Escape) GetTree().Quit();
        if (key.Keycode != Key.Key1 && key.Keycode != Key.Key2) return;
        _worldMode = key.Keycode == Key.Key2;
        LayoutStudy();
    }

    public override void _Process(double delta)
    {
        if (!_capture || _reference is null) return;
        _frames++;
        if (_frames == 8) SaveCapture("approved-art-reference-1080p.png");
        if (_frames == 9) { _worldMode = true; LayoutStudy(); }
        if (_frames == 16) { SaveCapture("approved-art-world-study-1080p.png"); GetTree().Quit(); }
    }

    private void SaveCapture(string filename)
    {
        string path = ProjectSettings.GlobalizePath("res://../docs/artifacts/" + filename);
        Error result = GetViewport().GetTexture().GetImage().SavePng(path);
        if (result != Error.Ok) throw new System.IO.IOException($"Study capture failed: {result}");
        GD.Print($"Art study: {path}");
    }
}

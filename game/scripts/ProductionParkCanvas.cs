using Godot;

namespace Zest.Game;

public enum StandUpgradeVisual { Base, BetterCounter, ElectricJuicer, BiggerCooler }
public enum StandOperatingVisual { Normal, SoldOut, RushMenu }

/// <summary>High-density ART-12 world assembled from independently layered production candidates.</summary>
public partial class ProductionParkCanvas : Node2D
{
    private const string FinalGridRoot = "res://art/production/final-grid-v02/";
    private const string StandGridRoot = "res://art/production/final-grid-v03/";
    public const string CustomerAssetRoot = "res://art/production/ai-layered-v01/customer-student-v01/";
    public const string CustomerVariantRoot = "res://art/production/ai-layered-v01/";

    /// <summary>Resolves the variant folder for a segment, falling back to the student set if the variant is missing.</summary>
    public static string ResolveCustomerAssetRoot(string? segmentId)
    {
        if (string.IsNullOrEmpty(segmentId) || segmentId == "student") return CustomerAssetRoot;
        string candidate = CustomerVariantRoot + $"customer-{segmentId}-v01/";
        return ResourceLoader.Exists(candidate + "customer-south-idle.png") ? candidate : CustomerAssetRoot;
    }
    public static readonly Vector2[] QueuePositions =
    [
        new(10, 34), new(10, 68), new(10, 102), new(10, 136),
        new(44, 136), new(44, 102), new(44, 68), new(44, 34),
    ];
    public ZestStandVisual Stand { get; private set; } = null!;
    private Sprite2D _background = null!;
    private Texture2D _dayBackground = null!;
    private Texture2D? _eveningBackground;
    private const string EveningBackgroundPath = FinalGridRoot + "bg_riverside_evening_640x360_v01.png";

    public void Build()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        _dayBackground = ResourceLoader.Load<Texture2D>(FinalGridRoot + "bg_riverside_640x360_v02.png");
        if (ResourceLoader.Exists(EveningBackgroundPath))
            _eveningBackground = ResourceLoader.Load<Texture2D>(EveningBackgroundPath);
        _background = new Sprite2D
        {
            Name = "RiversideBackground",
            Texture = _dayBackground,
            Centered = true,
            Scale = Vector2.One,
            TextureFilter = TextureFilterEnum.Nearest,
            ZIndex = -100,
        };
        AddChild(_background);

        // Opt-in proof path for the native Z-EN-01 ground; production remains on the
        // approved baked composition until a visual comparison is accepted.
        if (OS.GetEnvironment("ZEST_NATIVE_GROUND") == "1")
        {
            EnvironmentTileLayer nativeGround = new() { Name = "NativeGroundProof", Position = new Vector2(-320, -176), ZIndex = -99 };
            AddChild(nativeGround);
        }

        AddAnimatedProp("WestTree", FinalGridRoot + "prop_park_tree_idle_v02.png", new(-250, 14), 1, 0);
        AddAnimatedProp("EastTree", FinalGridRoot + "prop_park_tree_idle_v02.png", new(250, 92), 1, 0);
        AddAnimatedProp("ParkBench", FinalGridRoot + "prop_park_bench_idle_v02.png", new(-215, 44), 1, 0);
        AddAnimatedProp("LampSign", FinalGridRoot + "prop_lamp_sign_idle_v02.png", new(-132, 24), 1, 0);
        AddAnimatedProp("FlowerPlanter", FinalGridRoot + "prop_flower_planter_idle_v02.png", new(142, 35), 1, 0);
        AddAnimatedProp("WindBush", FinalGridRoot + "prop_bush_wind_4x1_v02.png", new(208, -16), 4, 3.5);
        TryAddOptionalProp("EditorialChalkboard", "sign_chalkboard_editorial_v01.png", new(-92, 14));
        TryAddOptionalProp("TrailSignpost", "sign_signpost_v01.png", new(268, 88));
        TryAddOptionalProp("StandMenuBoard", "sign_stand_menu_v01.png", new(72, -6));
        TryAddOptionalProp("PondDuck", "prop_park_duck_idle_v01.png", new(212, -144));
        TryAddOptionalProp("BenchSongbird", "prop_park_songbird_idle_v01.png", new(-194, -6));
        Stand = new ZestStandVisual { Name = "ZestStand", Position = new Vector2(0, 20), ZIndex = 1 };
        Stand.Configure(StandGridRoot);
        AddChild(Stand);
    }

    /// <summary>Swap between day and evening backgrounds. If the evening PNG is not present, stays on day.</summary>
    public void SetTimeOfDay(bool evening)
    {
        if (_background is null) return;
        Texture2D target = evening && _eveningBackground is not null ? _eveningBackground : _dayBackground;
        if (_background.Texture != target) _background.Texture = target;
    }

    /// <summary>Adds an idle prop only if the PNG exists — lets optional art land without editing this method.</summary>
    private Node2D? TryAddOptionalProp(string name, string filename, Vector2 foot)
    {
        string path = FinalGridRoot + filename;
        if (!ResourceLoader.Exists(path)) return null;
        return AddAnimatedProp(name, path, foot, 1, 0);
    }

    private Node2D AddAnimatedProp(string name, string texturePath, Vector2 foot, int columns, double fps)
    {
        Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
        int frameWidth = texture.GetWidth() / columns;
        int frameHeight = texture.GetHeight();
        Node2D root = new() { Name = name, Position = foot, ZIndex = 1 };
        Polygon2D shadow = new()
        {
            Name = "ContactShadow",
            Polygon = [new(-frameWidth * .34f, -4), new(frameWidth * .34f, -4), new(frameWidth * .43f, 2), new(-frameWidth * .43f, 2)],
            Color = new Color(ZestStyle.Palette.WorldShadow, .34f),
        };
        root.AddChild(shadow);

        SpriteFrames frames = new();
        frames.AddAnimation("idle");
        frames.SetAnimationLoopMode("idle", SpriteFrames.LoopMode.Linear);
        frames.SetAnimationSpeed("idle", fps > 0 ? fps : 1);
        for (int column = 0; column < columns; column++)
        {
            AtlasTexture frame = new()
            {
                Atlas = texture,
                Region = new Rect2(column * frameWidth, 0, frameWidth, frameHeight),
            };
            frames.AddFrame("idle", frame);
        }

        AnimatedSprite2D sprite = new()
        {
            Name = "AnimatedSprite",
            SpriteFrames = frames,
            Animation = "idle",
            Centered = true,
            Position = new Vector2(0, -frameHeight / 2f),
            Scale = Vector2.One,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        if (columns > 1) sprite.Play("idle");
        root.AddChild(sprite);
        AddChild(root);
        return root;
    }

    public static Label WorldLabel(string name, string text, Vector2 position, Color color, int fontSize)
    {
        Label label = new()
        {
            Name = name,
            Text = text,
            Position = position,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 30,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", ZestStyle.Palette.Charcoal);
        label.AddThemeConstantOverride("outline_size", 2);
        return label;
    }
}

/// <summary>ART-04 visual boundary for physical stand upgrades and readable operating states.</summary>
public partial class ZestStandVisual : Node2D
{
    private string _assetRoot = string.Empty;
    private Sprite2D _sprite = null!;
    private Node2D _layeredCandidate = null!;
    private VendorVisual _vendor = null!;
    private Node2D _plaque = null!;
    private Polygon2D _plaqueFace = null!;
    private PixelWorldText _plaqueLabel = null!;
    private PreparedBatchCue _batchCue = null!;
    private Sprite2D? _weatherOverlay;
    private Sprite2D? _vendorRainOverlay;
    private Sprite2D? _strongMenuFlag;
    private ProceduralAwning? _proceduralAwning;
    private ProceduralRainHat? _proceduralHat;
    private ProceduralStrongFlag? _proceduralFlag;
    private const string WeatherOverlayRoot = "res://art/production/ai-layered-v01/weather-overlays/";
    private const string StandOverlayRoot = "res://art/production/ai-layered-v01/stand-overlays/";
    private StandOperatingVisual _operatingState;
    private double _motionTime;

    public StandUpgradeVisual Upgrade { get; private set; }
    public StandOperatingVisual OperatingState => _operatingState;
    public int PreparedBatchCount { get; private set; }

    public void Configure(string assetRoot)
    {
        _assetRoot = assetRoot;
        Polygon2D shadow = new()
        {
            Name = "ContactShadow",
            Polygon = [new(-58, -4), new(58, -4), new(72, 2), new(-72, 2)],
            Color = new Color(ZestStyle.Palette.WorldShadow, .34f),
        };
        AddChild(shadow);

        _sprite = new Sprite2D
        {
            Name = "UpgradeSprite",
            Texture = ResourceLoader.Load<Texture2D>(_assetRoot + "prop_zest_stand_upgrades_4x1_v01.png"),
            RegionEnabled = true,
            RegionRect = new Rect2(0, 0, 170, 136),
            Centered = true,
            Position = new Vector2(0, -68),
            TextureFilter = TextureFilterEnum.Nearest,
        };
        AddChild(_sprite);

        _layeredCandidate = ResourceLoader.Load<PackedScene>("res://scenes/zest_stand_body.tscn").Instantiate<Node2D>();
        _layeredCandidate.Name = "ApprovedLayeredStand";
        AddChild(_layeredCandidate);
        _vendor = _layeredCandidate.GetNode<VendorVisual>("VendorAnchor/Vendor");

        _plaque = new Node2D { Name = "OperatingStatePlaque", Position = new Vector2(108, -84), ZIndex = 4, Visible = false };
        _plaqueFace = new Polygon2D
        {
            Polygon = [new(-34, -7), new(34, -7), new(32, 7), new(-32, 7)],
            Color = ZestStyle.Palette.Rust,
        };
        _plaque.AddChild(_plaqueFace);
        _plaqueLabel = new PixelWorldText { Name = "OperatingStateText", Position = new Vector2(0, -2) };
        _plaqueLabel.Configure("SOLD OUT", ZestStyle.Palette.Cream, centered: true);
        _plaque.AddChild(_plaqueLabel);
        AddChild(_plaque);

        _batchCue = new PreparedBatchCue { Name = "PreparedBatchCue", Position = new Vector2(104, -63), ZIndex = 5 };
        _batchCue.Configure();
        AddChild(_batchCue);
        SetPresentation(StandUpgradeVisual.Base, StandOperatingVisual.Normal, 0);
    }

    public void SetVendorPose(VendorPose pose) => _vendor.SetPose(pose);

    /// <summary>Weather-specific awning overlay: PNG when present, otherwise a procedural rim cue.</summary>
    public void SetWeatherOverlay(string? weatherId)
    {
        string? path = weatherId switch
        {
            "rain" => WeatherOverlayRoot + "awning-rain.png",
            "sunny" => WeatherOverlayRoot + "awning-sun.png",
            _ => null,
        };
        bool wantsPng = path is not null && ResourceLoader.Exists(path);
        if (path is null)
        {
            if (_weatherOverlay is not null) _weatherOverlay.Visible = false;
            if (_proceduralAwning is not null) _proceduralAwning.SetKind(ProceduralAwning.Kind.None);
            return;
        }
        if (wantsPng)
        {
            if (_proceduralAwning is not null) _proceduralAwning.SetKind(ProceduralAwning.Kind.None);
            if (_weatherOverlay is null)
            {
                _weatherOverlay = new Sprite2D
                {
                    Name = "WeatherOverlay",
                    Centered = true,
                    TextureFilter = TextureFilterEnum.Nearest,
                    ZIndex = 3,
                };
                AddChild(_weatherOverlay);
            }
            Texture2D weatherTex = ResourceLoader.Load<Texture2D>(path);
            _weatherOverlay.Texture = weatherTex;
            _weatherOverlay.Scale = weatherTex.GetHeight() > 300 ? Vector2.One * 0.11f : Vector2.One;
            _weatherOverlay.Position = weatherTex.GetHeight() > 300 ? new Vector2(0, -110) : new Vector2(0, -68);
            _weatherOverlay.Visible = true;
        }
        else
        {
            if (_weatherOverlay is not null) _weatherOverlay.Visible = false;
            if (_proceduralAwning is null)
            {
                _proceduralAwning = new ProceduralAwning { Name = "ProceduralAwning", Position = new Vector2(0, -95), ZIndex = 3 };
                AddChild(_proceduralAwning);
            }
            _proceduralAwning.SetKind(weatherId == "rain" ? ProceduralAwning.Kind.Rain : ProceduralAwning.Kind.Sun);
        }
    }

    /// <summary>Vendor rain-hat cue: PNG if present, otherwise a procedural little hat over the vendor head.</summary>
    public void SetVendorRainOverlay(bool active)
    {
        string path = WeatherOverlayRoot + "vendor-rain-hat.png";
        bool wantsPng = active && ResourceLoader.Exists(path);
        if (!active)
        {
            if (_vendorRainOverlay is not null) _vendorRainOverlay.Visible = false;
            if (_proceduralHat is not null) _proceduralHat.Visible = false;
            return;
        }
        if (wantsPng)
        {
            if (_proceduralHat is not null) _proceduralHat.Visible = false;
            if (_vendorRainOverlay is null)
            {
                _vendorRainOverlay = new Sprite2D
                {
                    Name = "VendorRainOverlay",
                    Centered = true,
                    Position = new Vector2(0, -46),
                    TextureFilter = TextureFilterEnum.Nearest,
                    ZIndex = 4,
                };
                AddChild(_vendorRainOverlay);
            }
            Texture2D hatTex = ResourceLoader.Load<Texture2D>(path);
            _vendorRainOverlay.Texture = hatTex;
            _vendorRainOverlay.Scale = hatTex.GetWidth() > 100
                ? Vector2.One * (22f / hatTex.GetWidth())
                : Vector2.One;
            _vendorRainOverlay.Visible = true;
        }
        else
        {
            if (_vendorRainOverlay is not null) _vendorRainOverlay.Visible = false;
            if (_proceduralHat is null)
            {
                _proceduralHat = new ProceduralRainHat { Name = "ProceduralRainHat", Position = new Vector2(0, -46), ZIndex = 4 };
                AddChild(_proceduralHat);
            }
            _proceduralHat.Visible = true;
        }
    }

    /// <summary>Strong menu cue: PNG if present, otherwise a procedural pennant flag.</summary>
    public void SetStrongMenuFlag(bool active)
    {
        string path = StandOverlayRoot + "strong-menu-flag.png";
        bool wantsPng = active && ResourceLoader.Exists(path);
        if (!active)
        {
            if (_strongMenuFlag is not null) _strongMenuFlag.Visible = false;
            if (_proceduralFlag is not null) _proceduralFlag.Visible = false;
            return;
        }
        if (wantsPng)
        {
            if (_proceduralFlag is not null) _proceduralFlag.Visible = false;
            if (_strongMenuFlag is null)
            {
                _strongMenuFlag = new Sprite2D
                {
                    Name = "StrongMenuFlag",
                    Centered = true,
                    Position = new Vector2(-58, -92),
                    TextureFilter = TextureFilterEnum.Nearest,
                    ZIndex = 5,
                };
                AddChild(_strongMenuFlag);
            }
            Texture2D flagTex = ResourceLoader.Load<Texture2D>(path);
            _strongMenuFlag.Texture = flagTex;
            _strongMenuFlag.Scale = flagTex.GetWidth() > 40
                ? Vector2.One * (28f / flagTex.GetWidth())
                : Vector2.One;
            _strongMenuFlag.Visible = true;
        }
        else
        {
            if (_strongMenuFlag is not null) _strongMenuFlag.Visible = false;
            if (_proceduralFlag is null)
            {
                _proceduralFlag = new ProceduralStrongFlag { Name = "ProceduralStrongFlag", Position = new Vector2(-58, -92), ZIndex = 5 };
                AddChild(_proceduralFlag);
            }
            _proceduralFlag.Visible = true;
        }
    }

    public void SetPresentation(StandUpgradeVisual upgrade, StandOperatingVisual operatingState, int preparedBatchCount)
    {
        Upgrade = upgrade;
        _operatingState = operatingState;
        PreparedBatchCount = Mathf.Clamp(preparedBatchCount, 0, 3);
        _sprite.RegionRect = new Rect2((int)upgrade * 170, 0, 170, 136);
        _layeredCandidate.Visible = true;
        _sprite.Visible = false;
        _plaque.Visible = operatingState != StandOperatingVisual.Normal;
        _plaqueLabel.SetText(operatingState == StandOperatingVisual.RushMenu ? "RUSH MENU" : "SOLD OUT");
        _plaqueFace.Color = operatingState == StandOperatingVisual.RushMenu ? ZestStyle.Palette.ZestYellow : ZestStyle.Palette.Rust;
        _plaqueLabel.SetColor(operatingState == StandOperatingVisual.RushMenu ? ZestStyle.Palette.Charcoal : ZestStyle.Palette.Cream);
        _batchCue.SetCount(PreparedBatchCount);
        SetProcess(_plaque.Visible || PreparedBatchCount > 0);
    }

    public override void _Process(double delta)
    {
        _motionTime += delta;
        _plaque.Position = new Vector2(108, -84 + Mathf.Round(Mathf.Sin((float)_motionTime * 2.4f)));
        _batchCue.SetSparkle(((int)(_motionTime * 3)) % 2 == 0);
    }
}

/// <summary>Three compact prepared-stock jars; intentionally drawable and animatable at final world pixels.</summary>
public partial class PreparedBatchCue : Node2D
{
    private int _count;
    private bool _sparkle;
    private PixelWorldText _label = null!;

    public void Configure()
    {
        _label = new PixelWorldText { Name = "PreparedBatchText", Position = new Vector2(0, -2) };
        _label.Configure("PREP +0", ZestStyle.Palette.Cream, centered: true);
        AddChild(_label);
    }

    public void SetCount(int count)
    {
        _count = Mathf.Clamp(count, 0, 3);
        Visible = _count > 0;
        _label.SetText($"PREP +{_count}");
        QueueRedraw();
    }

    public void SetSparkle(bool visible)
    {
        if (_sparkle == visible) return;
        _sparkle = visible;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-24, -8, 48, 16), ZestStyle.Palette.Charcoal);
        DrawRect(new Rect2(-23, -7, 46, 14), new Color("#4f7155"));
        if (_sparkle && _count > 0)
        {
            DrawRect(new Rect2(13, -5, 1, 1), ZestStyle.Palette.ZestYellow);
            DrawRect(new Rect2(14, -4, 1, 1), ZestStyle.Palette.Cream);
        }
    }
}

/// <summary>Native 5×7 world font: one logical pixel stays one 640×360 world pixel.</summary>
public partial class PixelWorldText : Node2D
{
    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['B'] = ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
        ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
        ['D'] = ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
        ['E'] = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        ['F'] = ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
        ['G'] = ["01111", "10000", "10000", "10111", "10001", "10001", "01111"],
        ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
        ['J'] = ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
        ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
        ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
        ['Q'] = ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
        ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
        ['W'] = ["10001", "10001", "10001", "10101", "10101", "11011", "10001"],
        ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
        ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
        ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        ['2'] = ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
        ['3'] = ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
        ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
        ['5'] = ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
        ['6'] = ["01110", "10000", "10000", "11110", "10001", "10001", "01110"],
        ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
        ['9'] = ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
        ['+'] = ["00000", "00100", "00100", "11111", "00100", "00100", "00000"],
        ['-'] = ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
        [' '] = ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
    };

    private string _text = string.Empty;
    private Color _color = Colors.White;
    private bool _centered;
    private Color _background = Colors.Transparent;

    public void Configure(string text, Color color, bool centered = false, Color? background = null)
    {
        _text = text.ToUpperInvariant();
        _color = color;
        _centered = centered;
        _background = background ?? Colors.Transparent;
        QueueRedraw();
    }

    public void SetText(string text)
    {
        _text = text.ToUpperInvariant();
        QueueRedraw();
    }

    public void SetColor(Color color)
    {
        _color = color;
        QueueRedraw();
    }

    public override void _Draw()
    {
        int width = Math.Max(0, _text.Length * 6 - 1);
        float originX = _centered ? -Mathf.Floor(width / 2f) : 0;
        if (_background.A > 0)
            DrawRect(new Rect2(originX - 3, -3, width + 6, 13), _background);
        DrawPass(originX + 1, 1, new Color(ZestStyle.Palette.Charcoal, .8f));
        DrawPass(originX, 0, _color);
    }

    private void DrawPass(float originX, float originY, Color color)
    {
        for (int glyphIndex = 0; glyphIndex < _text.Length; glyphIndex++)
        {
            if (!Glyphs.TryGetValue(_text[glyphIndex], out string[]? rows)) rows = Glyphs[' '];
            for (int y = 0; y < 7; y++)
            for (int x = 0; x < 5; x++)
            {
                if (rows[y][x] == '1') DrawRect(new Rect2(originX + glyphIndex * 6 + x, originY + y, 1, 1), color);
            }
        }
    }
}

/// <summary>HD 4×4 directional atlas actor, snapped to whole world pixels.</summary>
public partial class HdCustomerActor : Node2D
{
    private static readonly Vector2[] Route =
    [
        new(-286, 72), new(-58, 72), new(-58, 104), new(10, 104),
        new(10, 34), new(10, -18), new(116, -18), new(116, 76),
        new(282, 76), new(282, 150), new(-286, 150),
    ];

    private Sprite2D _sprite = null!;
    private Texture2D _south = null!;
    private Texture2D _southWalkA = null!;
    private Texture2D _southWalkB = null!;
    private Texture2D _north = null!;
    private Texture2D _northWalkB = null!;
    private Texture2D _west = null!;
    private Texture2D _westWalkB = null!;
    private Texture2D _receive = null!;
    private Texture2D _leave = null!;
    private Texture2D _southWestTurn = null!;
    private Texture2D _westSouthTurn = null!;
    private bool _walksRoute;
    private bool _servicePose;
    private int _routeIndex = 1;
    private double _animationTime;
    private double _queuePause;
    private double _leaveTimer;
    private bool _leaving;
    private double _turnTimer;
    private Texture2D? _turnTexture;
    private int _lastDirectionRow = 0;
    private Vector2 _precisePosition;
    private float _simulationSpeed = 1f;
    private bool _reducedMotion;
    public Guid CustomerId { get; private set; }
    private string? _segmentId;
    private const float RuntimeScale = .035f;
    private const float CanvasHeight = 1400;

    private static Color TintForSegment(string? segmentId) => segmentId switch
    {
        "commuter" => new Color(1.05f, 0.86f, 0.78f, 1f), // warm rust wash
        "tourist" => new Color(0.88f, 1.02f, 0.94f, 1f), // cool leaf wash
        _ => Colors.White,
    };

    public void Configure(bool walksRoute, int idleVariant, Guid customerId, int routeStartIndex = 0, string? segmentId = null)
    {
        string root = ProductionParkCanvas.ResolveCustomerAssetRoot(segmentId);
        _south = ResourceLoader.Load<Texture2D>(root + "customer-south-idle.png");
        _southWalkA = ResourceLoader.Load<Texture2D>(root + "customer-south-walk-a.png");
        _southWalkB = ResourceLoader.Load<Texture2D>(root + "customer-south-walk-b.png");
        _north = ResourceLoader.Load<Texture2D>(root + "customer-north-walk-a.png");
        _northWalkB = ResourceLoader.Load<Texture2D>(root + "customer-north-walk-b.png");
        _west = ResourceLoader.Load<Texture2D>(root + "customer-west-walk-a.png");
        _westWalkB = ResourceLoader.Load<Texture2D>(root + "customer-west-walk-b.png");
        _receive = ResourceLoader.Load<Texture2D>(root + "customer-south-receive.png");
        _leave = ResourceLoader.Load<Texture2D>(root + "customer-west-leave.png");
        _southWestTurn = ResourceLoader.Load<Texture2D>(root + "customer-south-west-turn.png");
        _westSouthTurn = ResourceLoader.Load<Texture2D>(root + "customer-west-south-turn.png");
        _walksRoute = walksRoute;
        CustomerId = customerId;
        _segmentId = segmentId;
        int startIndex = ((routeStartIndex % Route.Length) + Route.Length) % Route.Length;
        Position = walksRoute ? Route[startIndex] : Position;
        _routeIndex = walksRoute ? (startIndex + 1) % Route.Length : 1;
        _precisePosition = Position;
        _sprite = new Sprite2D
        {
            Name = "DirectionalSprite",
            Texture = _south,
            Centered = true,
            Position = new Vector2(0, -CanvasHeight * RuntimeScale / 2f),
            Scale = Vector2.One * RuntimeScale,
            FlipH = idleVariant % 2 != 0,
            TextureFilter = TextureFilterEnum.Nearest,
            Modulate = TintForSegment(_segmentId),
        };
        Polygon2D shadow = new()
        {
            Name = "ContactShadow",
            Polygon = [new(-9, 1), new(9, 1), new(7, 4), new(-7, 4)],
            Color = new Color(ZestStyle.Palette.WorldShadow, .32f),
            ZIndex = -1,
        };
        AddChild(shadow);
        AddChild(_sprite);
        SetProcess(walksRoute);
    }

    private PixelWorldText? _nameTag;
    private PixelWorldText? _orderBadge;

    public void SetOrderBadge(string? label, Color? textColor = null, Color? background = null)
    {
        if (string.IsNullOrEmpty(label))
        {
            _orderBadge?.QueueFree();
            _orderBadge = null;
            return;
        }
        Color text = textColor ?? ZestStyle.Palette.Charcoal;
        Color bg = background ?? new Color(ZestStyle.Palette.ZestYellow, .82f);
        if (_orderBadge is null)
        {
            _orderBadge = new PixelWorldText { Name = "OrderBadge", Position = new Vector2(14, -44), ZIndex = 21 };
            AddChild(_orderBadge);
        }
        _orderBadge.Configure(label, text, centered: true, background: bg);
    }

    public void SetNameTag(string? label)
    {
        if (string.IsNullOrEmpty(label))
        {
            _nameTag?.QueueFree();
            _nameTag = null;
            return;
        }
        if (_nameTag is null)
        {
            _nameTag = new PixelWorldText { Name = "NameTag", Position = new Vector2(0, -54), ZIndex = 20 };
            _nameTag.Configure(label, ZestStyle.Palette.Cream, centered: true, background: new Color(ZestStyle.Palette.Charcoal, .82f));
            AddChild(_nameTag);
        }
        else
        {
            _nameTag.SetText(label);
        }
    }

    public void SetSimulationSpeed(float speed) => _simulationSpeed = Mathf.Max(0, speed);

    public void SetReducedMotion(bool enabled)
    {
        _reducedMotion = enabled;
        if (enabled && _walksRoute) _sprite.Texture = _south;
    }

    public void SetServicePose(bool receive)
    {
        if (_walksRoute || _servicePose == receive) return;
        _servicePose = receive;
        _sprite.Texture = receive ? _receive : _south;
        _sprite.FlipH = false;
    }

    /// <summary>Plays the post-sale carry pose briefly, then removes this queue actor.</summary>
    public void PlayLeavePose() => PlayDeparturePose(abandoned: false);

    public void PlayDeparturePose(bool abandoned)
    {
        if (_walksRoute || _leaving || _sprite is null) return;
        _leaving = true;
        _leaveTimer = .8;
        _sprite.Texture = _leave;
        _sprite.FlipH = false;
        _sprite.Modulate = abandoned ? new Color("#d98a63") : Colors.White;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_reducedMotion && _walksRoute) return;
        delta *= _simulationSpeed;
        if (_leaving)
        {
            _leaveTimer -= delta;
            if (_leaveTimer <= 0) QueueFree();
            return;
        }
        _turnTimer = Math.Max(0, _turnTimer - delta);
        if (!_walksRoute) return;
        if (_queuePause > 0)
        {
            _queuePause -= delta;
            SetDirection(0, walking: false);
            return;
        }

        Vector2 target = Route[_routeIndex];
        Vector2 difference = target - Position;
        if (difference.Length() <= 1.5f)
        {
            Position = target;
            if (_routeIndex == 4) _queuePause = 2.25;
            _routeIndex = (_routeIndex + 1) % Route.Length;
            return;
        }

        Vector2 direction = difference.Normalized();
        _precisePosition += direction * 36f * (float)delta;
        Position = _precisePosition.Round();
        int row = Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)
            ? direction.X < 0 ? 1 : 2
            : direction.Y < 0 ? 3 : 0;
        _animationTime += delta;
        if (row != _lastDirectionRow)
        {
            _turnTexture = _lastDirectionRow == 0 && (row == 1 || row == 2)
                ? _southWestTurn
                : _lastDirectionRow is 1 or 2 && row == 0
                    ? _westSouthTurn
                    : null;
            _turnTimer = _turnTexture is null ? 0 : .12;
            _lastDirectionRow = row;
        }
        SetDirection(row, walking: true);
    }

    private void SetDirection(int row, bool walking)
    {
        bool alternate = walking && (int)(_animationTime * 8) % 2 == 1;
        if (_turnTimer > 0 && _turnTexture is not null)
        {
            _sprite.Texture = _turnTexture;
            _sprite.FlipH = row == 2;
            _sprite.Position = new Vector2(0, -CanvasHeight * RuntimeScale / 2f);
            return;
        }
        _sprite.Texture = row switch
        {
            3 => alternate ? _northWalkB : _north,
            1 or 2 => alternate ? _westWalkB : _west,
            _ => alternate ? _southWalkB : _south,
        };
        _sprite.FlipH = row == 2;
        float bob = walking && (int)(_animationTime * 8) % 4 is 1 or 2 ? -1 : 0;
        _sprite.Position = new Vector2(0, -CanvasHeight * RuntimeScale / 2f + bob);
    }
}

/// <summary>Palette-locked awning cue used until a painted PNG lands. Rain adds a river-blue rim with drips; sun adds a yellow glow rim.</summary>
public partial class ProceduralAwning : Node2D
{
    public enum Kind { None, Rain, Sun }
    private Kind _kind = Kind.None;
    private double _time;

    public void SetKind(Kind kind)
    {
        _kind = kind;
        Visible = kind != Kind.None;
        SetProcess(kind == Kind.Rain);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += delta;
        if (_kind == Kind.Rain) QueueRedraw();
    }

    public override void _Draw()
    {
        if (_kind == Kind.None) return;
        // Awning silhouette: matches the ZEST_YELLOW awning band on the stand.
        Color rim = _kind == Kind.Rain ? ZestStyle.Palette.River : ZestStyle.Palette.SunlitYellow;
        Vector2[] band =
        [
            new(-56, -3), new(56, -3), new(56, 3), new(-56, 3),
        ];
        DrawColoredPolygon(band, new Color(rim, 0.7f));
        if (_kind == Kind.Sun)
        {
            // Soft glow above the awning
            for (int i = 0; i < 4; i++)
                DrawRect(new Rect2(-56 + i * 30, -6 - i, 22, 1), new Color(ZestStyle.Palette.SunlitYellow, 0.28f - i * 0.05f));
        }
        else
        {
            // Drip pattern with subtle animated offset
            float phase = (float)(_time * 2.0);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 22;
                float dy = ((phase + i * 0.3f) % 1.0f) * 6f;
                DrawLine(new Vector2(x, 3), new Vector2(x - 1, 6 + dy), new Color(rim, 0.75f), 1f);
            }
        }
    }
}

/// <summary>Small drawn rain hat over the vendor's head when weather is rain and no PNG has landed.</summary>
public partial class ProceduralRainHat : Node2D
{
    public override void _Draw()
    {
        // Brim
        DrawColoredPolygon(new Vector2[] { new(-7, 0), new(7, 0), new(6, 2), new(-6, 2) }, ZestStyle.Palette.WorldWood);
        // Crown
        DrawColoredPolygon(new Vector2[] { new(-4, -4), new(4, -4), new(4, 0), new(-4, 0) }, ZestStyle.Palette.DeepWood);
        // Highlight
        DrawLine(new Vector2(-3, -4), new Vector2(3, -4), new Color(ZestStyle.Palette.WorldSkin, 0.4f), 1f);
    }
}

/// <summary>Amber pennant that reads STRONG once the recipe is on the menu.</summary>
public partial class ProceduralStrongFlag : Node2D
{
    private PixelWorldText? _label;

    public override void _Ready()
    {
        _label = new PixelWorldText { Name = "StrongLabel", Position = new Vector2(0, -6), ZIndex = 1 };
        _label.Configure("STRONG", ZestStyle.Palette.Charcoal, centered: true);
        AddChild(_label);
    }

    public override void _Draw()
    {
        // Pole
        DrawLine(new Vector2(-20, 0), new Vector2(-20, -18), ZestStyle.Palette.WorldWood, 1f);
        // Pennant
        DrawColoredPolygon(new Vector2[]
        {
            new(-20, -18), new(20, -14), new(-20, -10),
        }, ZestStyle.Palette.Amber);
    }
}

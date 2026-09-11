using Godot;

namespace Zest.Game;

/// <summary>Draws the tile field and owns all foot-pivoted, Y-sorted ART-12 props.</summary>
public partial class PixelParkCanvas : Node2D
{
    public const string CustomerTexturePath = "res://art/pixel/characters/chr_guest_base_walk_v01.png";
    public static readonly Vector2[] QueuePositions =
    [
        new(20, 32), new(20, 56), new(20, 80), new(20, 104),
        new(20, 128), new(48, 128), new(48, 104), new(48, 80),
    ];

    private Texture2D _grass = null!;
    private Texture2D _pathEdge = null!;

    public void Build()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        _grass = ResourceLoader.Load<Texture2D>("res://art/pixel/tiles/tile_grass_base_idle_v01.png");
        _pathEdge = ResourceLoader.Load<Texture2D>("res://art/pixel/tiles/tile_path_edge_base_idle_v01.png");

        AddProp("BackTreeWest", "res://art/pixel/props/prop_park_tree_leafy_idle_v01.png", new(-236, -72));
        AddProp("BackTreeEast", "res://art/pixel/props/prop_park_tree_leafy_idle_v01.png", new(232, -78));
        AddProp("ZestStand", "res://art/pixel/props/prop_zest_stand_base_idle_v01.png", new(0, 18));
        AddProp("LemonCrate", "res://art/pixel/props/prop_lemon_crate_idle_v01.png", new(-102, 24));
        AddProp("Bench", "res://art/pixel/props/prop_park_bench_wood_idle_v01.png", new(-202, 18));
        AddProp("LampSign", "res://art/pixel/props/prop_park_lamp_sign_idle_v01.png", new(158, 22));
        AddProp("Planter", "res://art/pixel/props/prop_park_planter_flowers_idle_v01.png", new(-132, 22));
        AddProp("FrontTreeWest", "res://art/pixel/props/prop_park_tree_leafy_idle_v01.png", new(-272, 156));
        AddProp("FrontTreeEast", "res://art/pixel/props/prop_park_tree_leafy_idle_v01.png", new(268, 154));
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-320, -180, 640, 360), ZestStyle.Palette.WorldSky);
        DrawRect(new Rect2(-320, -180, 640, 54), ZestStyle.Palette.River);
        for (int y = -126; y < 180; y += 16)
        for (int x = -320; x < 320; x += 16)
            DrawTexture(_grass, new Vector2(x, y));

        DrawRect(new Rect2(-320, 30, 640, 78), ZestStyle.Palette.WorldPath);
        DrawRect(new Rect2(4, 6, 60, 150), ZestStyle.Palette.Paper);
        for (int x = -320; x < 320; x += 16)
        {
            DrawTexture(_pathEdge, new Vector2(x, 14));
            DrawTexture(_pathEdge, new Vector2(x, 108));
        }

        for (int x = -304; x < 320; x += 42)
        {
            DrawRect(new Rect2(x, -162 + (x % 3), 18, 2), new Color(ZestStyle.Palette.Cream, .32f));
            DrawRect(new Rect2(x + 8, -146, 10, 2), new Color(ZestStyle.Palette.Cream, .2f));
        }

        DrawRect(new Rect2(-320, -128, 640, 4), ZestStyle.Palette.DeepWood);
        for (int x = -304; x < 320; x += 28)
        {
            DrawRect(new Rect2(x, -131, 18, 7), ZestStyle.Palette.Stone);
            DrawRect(new Rect2(x + 3, -132, 12, 2), ZestStyle.Palette.Cream);
        }
        for (int x = -285; x < 300; x += 92)
        {
            DrawRect(new Rect2(x, -160, 24, 5), new Color(ZestStyle.Palette.WaterHighlight, .72f));
            DrawRect(new Rect2(x + 8, -148, 15, 3), new Color(ZestStyle.Palette.Cream, .28f));
        }
        DrawRect(new Rect2(246, -177, 12, 48), ZestStyle.Palette.DeepWood);
        DrawRect(new Rect2(302, -177, 12, 48), ZestStyle.Palette.DeepWood);
        DrawRect(new Rect2(240, -178, 80, 13), ZestStyle.Palette.Amber);
        for (int x = 244; x < 318; x += 14) DrawRect(new Rect2(x, -176, 2, 10), ZestStyle.Palette.DeepWood);

        DrawFlowerPatch(new Vector2(-164, -54));
        DrawFlowerPatch(new Vector2(112, -72));
        DrawFlowerPatch(new Vector2(-116, 142));
        DrawFlowerPatch(new Vector2(168, 146));
        DrawRock(new Vector2(-286, -82));
        DrawRock(new Vector2(260, 124));

        DrawRect(new Rect2(-320, 28, 640, 2), ZestStyle.Palette.Ink);
        DrawRect(new Rect2(-320, 107, 640, 2), ZestStyle.Palette.Ink);
        DrawRect(new Rect2(-78, -2, 156, 3), new Color(ZestStyle.Palette.ZestYellow, .55f));

        foreach (Vector2 marker in QueuePositions)
        {
            DrawArc(marker + new Vector2(0, 1), 10, 0, Mathf.Tau, 12, ZestStyle.Palette.Cream, 2, false);
        }

        DrawString(ThemeDB.FallbackFont, new Vector2(-282, -136), "RIVERSIDE PARK", HorizontalAlignment.Left, -1, 8, ZestStyle.Palette.Cream);
        DrawString(ThemeDB.FallbackFont, new Vector2(-282, 166), "WEST GATE  →  MARKET WALK", HorizontalAlignment.Left, -1, 7, ZestStyle.Palette.Charcoal);
    }

    private Node2D AddProp(string name, string path, Vector2 foot, int zIndex = 1)
    {
        Texture2D texture = ResourceLoader.Load<Texture2D>(path);
        Node2D root = new() { Name = name, Position = foot, ZIndex = zIndex };
        Polygon2D shadow = new()
        {
            Name = "FootShadow",
            Polygon = [new(-texture.GetWidth() * .34f, -4), new(texture.GetWidth() * .34f, -4), new(texture.GetWidth() * .42f, 0), new(-texture.GetWidth() * .42f, 0)],
            Color = new Color(ZestStyle.Palette.WorldShadow, .42f),
        };
        root.AddChild(shadow);
        Sprite2D sprite = new()
        {
            Name = "Sprite",
            Texture = texture,
            Centered = false,
            Position = new Vector2(-texture.GetWidth() / 2f, -texture.GetHeight()),
            TextureFilter = TextureFilterEnum.Nearest,
        };
        root.AddChild(sprite);
        AddChild(root);
        return root;
    }

    private void DrawFlowerPatch(Vector2 at)
    {
        DrawRect(new Rect2(at.X, at.Y + 7, 24, 4), ZestStyle.Palette.DeepLeaf);
        DrawRect(new Rect2(at.X + 3, at.Y + 2, 3, 8), ZestStyle.Palette.Leaf);
        DrawRect(new Rect2(at.X + 12, at.Y, 3, 10), ZestStyle.Palette.Moss);
        DrawRect(new Rect2(at.X + 20, at.Y + 3, 3, 7), ZestStyle.Palette.Leaf);
        DrawRect(new Rect2(at.X + 1, at.Y, 5, 4), ZestStyle.Palette.Cream);
        DrawRect(new Rect2(at.X + 10, at.Y - 2, 5, 4), ZestStyle.Palette.ZestYellow);
        DrawRect(new Rect2(at.X + 18, at.Y + 1, 5, 4), ZestStyle.Palette.Cream);
    }

    private void DrawRock(Vector2 at)
    {
        DrawRect(new Rect2(at.X, at.Y + 4, 20, 10), ZestStyle.Palette.WorldShadow);
        DrawRect(new Rect2(at.X + 3, at.Y, 14, 12), ZestStyle.Palette.Stone);
        DrawRect(new Rect2(at.X + 6, at.Y + 1, 8, 3), ZestStyle.Palette.Cream);
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

/// <summary>One ART-10 4×4 sheet actor with integer-position route movement.</summary>
public partial class PixelCustomerActor : Node2D
{
    private static readonly Vector2[] Route =
    [
        new(-236, 72), new(-20, 72), new(-20, 56), new(20, 56),
        new(20, 12), new(118, 12), new(118, 82), new(288, 82),
        new(288, 132), new(-236, 132),
    ];

    private Sprite2D _sprite = null!;
    private bool _walksRoute;
    private int _routeIndex = 1;
    private double _animationTime;
    private double _queuePause;

    public void Configure(Texture2D texture, bool walksRoute, int idleFrame)
    {
        _walksRoute = walksRoute;
        Position = walksRoute ? Route[0] : Position;
        _sprite = new Sprite2D
        {
            Name = "DirectionalSprite",
            Texture = texture,
            Hframes = 4,
            Vframes = 4,
            Frame = idleFrame,
            Centered = false,
            Position = new Vector2(-8, -23),
            TextureFilter = TextureFilterEnum.Nearest,
        };
        AddChild(_sprite);
        SetProcess(walksRoute);
    }

    public override void _Process(double delta)
    {
        if (!_walksRoute) return;
        if (_queuePause > 0)
        {
            _queuePause -= delta;
            _sprite.Frame = 0;
            return;
        }

        Vector2 target = Route[_routeIndex];
        Vector2 difference = target - Position;
        if (difference.Length() <= 1.5f)
        {
            Position = target;
            if (_routeIndex == 3) _queuePause = 2.25;
            _routeIndex = (_routeIndex + 1) % Route.Length;
            return;
        }

        Vector2 direction = difference.Normalized();
        Position = (Position + direction * 34f * (float)delta).Round();
        int row = Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)
            ? direction.X < 0 ? 1 : 2
            : direction.Y < 0 ? 3 : 0;
        _animationTime += delta;
        _sprite.Frame = row * 4 + (int)(_animationTime * 8) % 4;
    }
}

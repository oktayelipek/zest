using Godot;

namespace Zest.Game;

/// <summary>ART-02 production tokens shared by world presentation and native-resolution UI.</summary>
public static class ZestStyle
{
    public static class Palette
    {
        public static readonly Color Ink = new("#292621");
        public static readonly Color Charcoal = new("#253333");
        public static readonly Color Paper = new("#f2ead8");
        public static readonly Color Cream = new("#fff8e8");
        public static readonly Color ZestYellow = new("#e8b447");
        public static readonly Color Leaf = new("#4f7155");
        public static readonly Color Rust = new("#a65338");
        public static readonly Color River = new("#557c78");
        public static readonly Color ProductBerry = new("#934f64");
        public static readonly Color MutedInk = new("#665e53");
        public static readonly Color Border = new("#c7b79f");
        public static readonly Color HudMuted = new("#b9c8be");

        public static readonly Color WorldSky = new("#d9dfc7");
        public static readonly Color WorldGround = new("#91a76f");
        public static readonly Color WorldPath = new("#d8c9aa");
        public static readonly Color WorldQueuePath = new("#c5b18d");
        public static readonly Color WorldWood = new("#71543a");
        public static readonly Color WorldSkin = new("#e7b990");
        public static readonly Color DeepLeaf = new("#2f4a3c");
        public static readonly Color Moss = new("#738c55");
        public static readonly Color LeafHighlight = new("#a6b85e");
        public static readonly Color DeepWood = new("#4a3528");
        public static readonly Color Amber = new("#d68e2e");
        public static readonly Color WaterHighlight = new("#7fa6a0");
        public static readonly Color WorldShadow = new("#3f4d3d");
        public static readonly Color Stone = new("#a49a85");
        public static readonly Color SunlitYellow = new("#eacb6a");
    }

    public static class Type
    {
        public const int Display = 40;
        public const int Brand = 30;
        public const int Metric = 23;
        public const int Body = 17;
        public const int Action = 15;
        public const int Label = 12;
        public const int Micro = 11;
    }

    public static class Space
    {
        public const int Xs = 4;
        public const int Sm = 8;
        public const int Md = 12;
        public const int Lg = 18;
        public const int Xl = 28;
        public const int Page = 42;
    }

    public static class Radius
    {
        public const int Control = 8;
        public const int Panel = 14;
        public const int Pill = 18;
    }

    public static class PixelRendering
    {
        public static readonly Vector2I BaseResolution = new(1920, 1080);
        public static readonly Vector2I LogicalWorldResolution = new(640, 360);
        public const int WorldRenderScale = 3;
        public const int SourceTilePixels = 16;
        public const float WorldSnapIncrement = 1f / 16f;
    }
}

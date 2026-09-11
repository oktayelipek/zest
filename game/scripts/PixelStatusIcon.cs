using Godot;

namespace Zest.Game;

public enum HudGlyph { Clock, Sun, Coin, Reputation, Lemon, Berry }

/// <summary>Small code-native pixel glyph used beside native-resolution HUD typography.</summary>
public partial class PixelStatusIcon : Control
{
    public HudGlyph Glyph { get; init; }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(22, 22);
        MouseFilter = MouseFilterEnum.Ignore;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color yellow = ZestStyle.Palette.ZestYellow;
        Color cream = ZestStyle.Palette.Cream;
        Color leaf = ZestStyle.Palette.LeafHighlight;
        Color river = ZestStyle.Palette.WaterHighlight;
        switch (Glyph)
        {
            case HudGlyph.Clock:
                Pixel(5, 4, 12, 2, cream); Pixel(3, 6, 16, 10, cream); Pixel(5, 16, 12, 2, cream);
                Pixel(10, 7, 2, 5, river); Pixel(11, 11, 4, 2, river);
                break;
            case HudGlyph.Sun:
                Pixel(8, 8, 6, 6, yellow); Pixel(10, 3, 2, 3, yellow); Pixel(10, 16, 2, 3, yellow);
                Pixel(3, 10, 3, 2, yellow); Pixel(16, 10, 3, 2, yellow);
                break;
            case HudGlyph.Coin:
                Pixel(6, 4, 10, 2, yellow); Pixel(4, 6, 14, 10, yellow); Pixel(6, 16, 10, 2, yellow);
                Pixel(9, 7, 5, 2, cream); Pixel(8, 10, 5, 2, cream); Pixel(8, 13, 5, 2, cream);
                break;
            case HudGlyph.Reputation:
                Pixel(5, 10, 7, 7, leaf); Pixel(10, 5, 7, 7, leaf); Pixel(10, 10, 2, 8, cream);
                break;
            case HudGlyph.Lemon:
                Pixel(5, 8, 12, 8, yellow); Pixel(7, 6, 8, 12, yellow);
                Pixel(15, 5, 4, 3, leaf); Pixel(17, 3, 2, 4, leaf); Pixel(8, 9, 2, 2, cream);
                break;
            case HudGlyph.Berry:
                Color berry = ZestStyle.Palette.ProductBerry;
                Pixel(5, 8, 6, 6, berry); Pixel(10, 6, 6, 6, berry); Pixel(11, 12, 6, 6, berry);
                Pixel(8, 4, 8, 3, leaf); Pixel(11, 3, 2, 5, leaf); Pixel(7, 9, 2, 2, cream);
                break;
        }
    }

    private void Pixel(float x, float y, float width, float height, Color color) =>
        DrawRect(new Rect2(x, y, width, height), color, filled: true, width: -1, antialiased: false);
}

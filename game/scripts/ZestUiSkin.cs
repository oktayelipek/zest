using Godot;

namespace Zest.Game;

/// <summary>UI-11 reusable hybrid-pixel component skin shared by live and editorial surfaces.</summary>
public static class ZestUiSkin
{
    public static Theme CreateTheme()
    {
        Theme theme = new();
        theme.SetStylebox("panel", "TooltipPanel", Frame(new Color(ZestStyle.Palette.Charcoal, .97f), ZestStyle.Radius.Control, 9, 1, ZestStyle.Palette.River));
        theme.SetColor("font_color", "TooltipLabel", ZestStyle.Palette.Cream);
        theme.SetFontSize("font_size", "TooltipLabel", ZestStyle.Type.Action);
        return theme;
    }

    public static void ApplyButton(Button button, Color fill, Color textColor)
    {
        button.FocusMode = Control.FocusModeEnum.All;
        button.AddThemeFontSizeOverride("font_size", ZestStyle.Type.Action);
        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeColorOverride("font_focus_color", textColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(ZestStyle.Palette.MutedInk, .72f));
        button.AddThemeStyleboxOverride("normal", Frame(fill, ZestStyle.Radius.Control, 8, 1, ZestStyle.Palette.Border));
        button.AddThemeStyleboxOverride("hover", Frame(fill.Lightened(.08f), ZestStyle.Radius.Control, 8, 2, ZestStyle.Palette.ZestYellow));
        button.AddThemeStyleboxOverride("pressed", Frame(fill.Darkened(.09f), ZestStyle.Radius.Control, 8, 2, ZestStyle.Palette.Ink));
        button.AddThemeStyleboxOverride("disabled", Frame(new Color(fill, .56f), ZestStyle.Radius.Control, 8, 1, new Color(ZestStyle.Palette.Border, .55f)));
        button.AddThemeStyleboxOverride("focus", Frame(new Color(0, 0, 0, 0), ZestStyle.Radius.Control, 6, 2, ZestStyle.Palette.ZestYellow));
    }

    public static PanelContainer Panel(Color fill, int radius = ZestStyle.Radius.Control, int padding = 12)
    {
        PanelContainer panel = new() { ZIndex = 10 };
        panel.AddThemeStyleboxOverride("panel", Frame(fill, radius, padding, 1, new Color(ZestStyle.Palette.Border, .7f)));
        return panel;
    }

    public static StyleBoxFlat Frame(Color fill, int radius, int padding, int border, Color borderColor)
    {
        StyleBoxFlat box = new() { BgColor = fill, BorderColor = borderColor, AntiAliasing = false };
        box.SetCornerRadiusAll(radius);
        box.SetContentMarginAll(padding);
        box.SetBorderWidthAll(border);
        return box;
    }

    public static void Tooltip(Control control, string text) => control.TooltipText = text;
}

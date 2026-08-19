using Godot;

namespace ShallowSeaDream;

/// Restrained UI system for the hand-painted underwater storybook. Controls use a
/// small number of editorial surfaces and a 4pt spatial scale; the scenery remains
/// the visual hero instead of every HUD element competing with a glowing outline.
public static class UiTheme
{
    public const string FontPath = "res://assets/fonts/AaShuiyu.ttf";
    private static FontFile? _font;

    public static FontFile? Font
    {
        get
        {
            if (_font == null && ResourceLoader.Exists(FontPath))
                _font = GD.Load<FontFile>(FontPath);
            return _font;
        }
    }

    public const int FontDisplay = 104;
    public const int FontH1 = 48;
    public const int FontH2 = 30;
    public const int FontBody = 24;
    public const int FontSmall = 18;
    public const int FontTiny = 15;

    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space6 = 24;
    public const int Space8 = 32;
    public const int Space12 = 48;
    public const int SafeArea = 72;

    public static readonly Color Ink = new(0.86f, 0.96f, 1.0f);
    public static readonly Color InkDim = new(0.62f, 0.78f, 0.86f);
    public static readonly Color InkFaint = new(0.52f, 0.68f, 0.76f, 0.7f);
    public static readonly Color Accent = Palette.CoastalCyan;
    public static readonly Color Teal = Palette.PollutedTeal;
    public static readonly Color Hairline = new(0.52f, 0.94f, 1.0f, 0.26f);
    public static readonly Color GlassBg = new(0.025f, 0.075f, 0.105f, 0.88f);
    public static readonly Color GlassBgDeep = new(0.03f, 0.09f, 0.14f, 0.94f);
    public static readonly Color Shadow = new(0.0f, 0.02f, 0.04f, 0.38f);

    /// Quiet storybook surface. The legacy method name is retained so older panels
    /// inherit the refined treatment without bespoke style code.
    public static StyleBoxFlat GlassPanel(int radius = 10, float bgAlpha = 0.82f, int pad = 20)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(GlassBg.R, GlassBg.G, GlassBg.B, bgAlpha),
            BorderColor = Hairline,
            ShadowColor = Shadow,
            ShadowSize = 6,
            ShadowOffset = new Vector2(0, 3),
            AntiAliasing = true,
        };
        sb.SetCornerRadiusAll(radius);
        sb.SetBorderWidthAll(1);
        sb.SetContentMarginAll(pad);
        return sb;
    }

    public static StyleBoxFlat OverlayPanel(int radius = 12, int pad = 32)
    {
        var sb = GlassPanel(radius, 0.94f, pad);
        sb.BgColor = GlassBgDeep;
        sb.ShadowSize = 12;
        return sb;
    }

    public static StyleBoxFlat Scrim(float alpha = 0.72f) =>
        new() { BgColor = new Color(0.02f, 0.06f, 0.10f, alpha) };

    private static StyleBoxFlat ButtonBase(Color bg, Color border)
    {
        var sb = new StyleBoxFlat { BgColor = bg, BorderColor = border, AntiAliasing = true };
        sb.SetCornerRadiusAll(9);
        sb.SetBorderWidthAll(1);
        sb.ContentMarginLeft = 24;
        sb.ContentMarginRight = 24;
        sb.ContentMarginTop = 14;
        sb.ContentMarginBottom = 14;
        return sb;
    }

    public static StyleBoxFlat PillNormal() =>
        ButtonBase(new Color(Accent.R, Accent.G, Accent.B, 0.92f),
                   new Color(Accent.R, Accent.G, Accent.B, 0.75f));

    public static StyleBoxFlat PillHover()
    {
        var sb = ButtonBase(new Color(0.72f, 0.96f, 0.96f, 1f),
                            new Color(0.82f, 1f, 1f, 0.9f));
        sb.ShadowColor = new Color(0f, 0.03f, 0.05f, 0.28f);
        sb.ShadowSize = 6;
        return sb;
    }

    public static StyleBoxFlat PillPressed() =>
        ButtonBase(new Color(0.56f, 0.85f, 0.86f, 1f), Accent);

    public static StyleBoxFlat PillDisabled() =>
        ButtonBase(new Color(0.3f, 0.4f, 0.45f, 0.10f),
                   new Color(0.4f, 0.5f, 0.55f, 0.25f));

    private static StyleBoxFlat SecondaryNormal() =>
        ButtonBase(new Color(0.02f, 0.08f, 0.11f, 0.46f),
                   new Color(Accent.R, Accent.G, Accent.B, 0.30f));

    public static Button StyleButton(Button btn, int fontSize = FontBody, bool primary = true)
    {
        btn.CustomMinimumSize = new Vector2(0, 64);
        btn.AddThemeStyleboxOverride("normal", primary ? PillNormal() : SecondaryNormal());
        btn.AddThemeStyleboxOverride("hover", PillHover());
        btn.AddThemeStyleboxOverride("pressed", PillPressed());
        btn.AddThemeStyleboxOverride("focus", PillHover());
        btn.AddThemeStyleboxOverride("disabled", PillDisabled());
        btn.AddThemeColorOverride("font_color", primary ? new Color(0.03f, 0.13f, 0.16f) : Ink);
        btn.AddThemeColorOverride("font_hover_color", new Color(0.02f, 0.10f, 0.13f));
        btn.AddThemeColorOverride("font_pressed_color", new Color(0.02f, 0.10f, 0.13f));
        btn.AddThemeColorOverride("font_focus_color", new Color(0.02f, 0.10f, 0.13f));
        btn.AddThemeColorOverride("font_disabled_color", InkFaint);
        ApplyFont(btn, fontSize);
        UiFx.AnimateButton(btn);
        return btn;
    }

    public static void ApplyFont(Control c, int size)
    {
        if (Font != null) c.AddThemeFontOverride("font", Font);
        c.AddThemeFontSizeOverride("font_size", size);
    }

    public static Label MakeLabel(string text, int size, Color color, bool wrap = false)
    {
        var l = new Label { Text = text };
        if (wrap) l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeConstantOverride("line_spacing", Mathf.RoundToInt(size * 0.32f));
        ApplyFont(l, size);
        return l;
    }

    public static ColorRect Divider(float alpha = 0.18f)
    {
        var r = new ColorRect
        {
            Color = new Color(Accent.R, Accent.G, Accent.B, alpha),
            CustomMinimumSize = new Vector2(0, 1),
        };
        r.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return r;
    }

    public static Panel MakeGlassPanel(string name, int radius = 10, float bgAlpha = 0.82f, int pad = 20)
    {
        var p = new Panel { Name = name };
        p.AddThemeStyleboxOverride("panel", GlassPanel(radius, bgAlpha, pad));
        return p;
    }

    public static Theme BuildTheme()
    {
        var theme = new Theme();
        if (Font != null) theme.DefaultFont = Font;
        theme.DefaultFontSize = FontBody;
        theme.SetColor("font_color", "Label", Ink);
        theme.SetColor("font_color", "Button", Ink);
        theme.SetStylebox("normal", "Button", SecondaryNormal());
        theme.SetStylebox("hover", "Button", PillHover());
        theme.SetStylebox("pressed", "Button", PillPressed());
        theme.SetStylebox("disabled", "Button", PillDisabled());
        theme.SetStylebox("panel", "Panel", GlassPanel());
        theme.SetStylebox("panel", "PanelContainer", GlassPanel());
        return theme;
    }
}

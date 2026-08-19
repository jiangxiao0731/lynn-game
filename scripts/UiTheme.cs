using System.Collections.Generic;
using Godot;

namespace ShallowSeaDream;

/// Hand-painted underwater storybook UI. Every visible surface is rendered from a
/// small deterministic watercolor texture with fibrous variation and imperfect ink
/// edges. No image assets are generated or written: the textures live in memory.
public static class UiTheme
{
    public const string FontPath = "res://assets/fonts/AaShuiyu.ttf";
    private static FontFile? _displayFont;
    private static SystemFont? _bodyFont;
    private static SystemFont? _strongFont;
    private static readonly Dictionary<string, Texture2D> PaintedTextures = new();
    private static readonly Dictionary<string, Texture2D> BrushTextures = new();

    /// The supplied hand-drawn face is reserved for expressive display text.
    public static FontFile? DisplayFont
    {
        get
        {
            if (_displayFont == null && ResourceLoader.Exists(FontPath))
                _displayFont = GD.Load<FontFile>(FontPath);
            return _displayFont;
        }
    }

    /// A calm, highly legible editorial face for paragraphs, HUD data and controls.
    /// It uses fonts already installed on the viewing system and falls back safely.
    public static SystemFont BodyFont => _bodyFont ??= MakeSystemFont(450);
    public static SystemFont StrongFont => _strongFont ??= MakeSystemFont(650);

    // A deliberately contrasted type scale: display / heading / lead / body / meta.
    public const int FontDisplay = 92;
    public const int FontH1 = 46;
    public const int FontH2 = 30;
    public const int FontBody = 21;
    public const int FontSmall = 17;
    public const int FontTiny = 13;

    private static SystemFont MakeSystemFont(int weight) => new()
    {
        FontNames = new[] { "Avenir Next", "Avenir", "Helvetica Neue", "Arial" },
        FontWeight = weight,
        AllowSystemFallback = true,
        MultichannelSignedDistanceField = true,
    };

    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space6 = 24;
    public const int Space8 = 32;
    public const int Space12 = 48;
    public const int SafeArea = 72;

    public static readonly Color Ink = new(0.88f, 0.96f, 0.89f);
    public static readonly Color InkDim = new(0.66f, 0.82f, 0.76f);
    public static readonly Color InkFaint = new(0.54f, 0.70f, 0.66f, 0.76f);
    public static readonly Color Accent = new(0.58f, 0.91f, 0.76f);
    public static readonly Color Teal = Palette.PollutedTeal;
    public static readonly Color Hairline = new(0.46f, 0.74f, 0.64f, 0.88f);
    public static readonly Color GlassBg = new(0.045f, 0.12f, 0.14f, 0.94f);
    public static readonly Color GlassBgDeep = new(0.035f, 0.09f, 0.11f, 0.98f);
    public static readonly Color Shadow = new(0.0f, 0.02f, 0.04f, 0.38f);

    private static float Hash01(int x, int y, int seed)
    {
        uint h = unchecked((uint)(x * 374761393 + y * 668265263 + seed * 69069));
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h & 0xffffu) / 65535f;
    }

    private static float Wobble(float value, int seed) =>
        Mathf.Sin(value * 0.117f + seed * 0.73f) * 1.9f
        + Mathf.Sin(value * 0.041f + seed * 1.31f) * 1.25f;

    private static Texture2D PaintedTexture(Color wash, Color edge, int seed)
    {
        string key = $"{wash.ToHtml()}_{edge.ToHtml()}_{seed}";
        if (PaintedTextures.TryGetValue(key, out var cached)) return cached;

        const int size = 128;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float left = 7f + Wobble(y, seed);
            float right = 120f + Wobble(y, seed + 17);
            float top = 7f + Wobble(x, seed + 31);
            float bottom = 120f + Wobble(x, seed + 53);
            if (x < left || x > right || y < top || y > bottom)
            {
                image.SetPixel(x, y, Colors.Transparent);
                continue;
            }

            float edgeDistance = Mathf.Min(Mathf.Min(x - left, right - x), Mathf.Min(y - top, bottom - y));
            float grain = (Hash01(x / 3, y / 3, seed + 91) - .5f) * .16f
                        + Mathf.Sin((x + y) * .071f + seed) * .025f;
            float factor = 1f + grain;
            var paper = new Color(
                Mathf.Clamp(wash.R * factor, 0f, 1f), Mathf.Clamp(wash.G * factor, 0f, 1f),
                Mathf.Clamp(wash.B * factor, 0f, 1f), wash.A * (.92f + Hash01(x, y, seed + 7) * .08f));
            float edgeMix = 1f - Mathf.SmoothStep(.5f, 4.5f, edgeDistance);
            if (Hash01(x, y, seed + 111) > .965f) paper = paper.Lightened(.08f);
            image.SetPixel(x, y, paper.Lerp(edge, edgeMix * .92f));
        }
        var texture = ImageTexture.CreateFromImage(image);
        PaintedTextures[key] = texture;
        return texture;
    }

    private static StyleBoxTexture PaintedStyle(Color wash, Color edge, int pad, int seed)
    {
        var style = new StyleBoxTexture
        {
            Texture = PaintedTexture(wash, edge, seed),
            TextureMarginLeft = 28, TextureMarginTop = 28,
            TextureMarginRight = 28, TextureMarginBottom = 28,
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch,
            DrawCenter = true,
        };
        style.SetContentMarginAll(pad);
        return style;
    }

    /// Quiet storybook surface. The legacy method name is retained so older panels
    /// inherit the refined treatment without bespoke style code.
    public static StyleBoxTexture GlassPanel(int radius = 10, float bgAlpha = 0.82f, int pad = 20)
    {
        var wash = new Color(GlassBg.R, GlassBg.G, GlassBg.B, Mathf.Clamp(bgAlpha + .08f, 0f, .98f));
        return PaintedStyle(wash, Hairline, pad, 19 + radius);
    }

    public static StyleBoxTexture OverlayPanel(int radius = 12, int pad = 32) =>
        PaintedStyle(GlassBgDeep, new Color(.62f,.82f,.69f,.96f), pad, 71 + radius);

    public static StyleBoxFlat Scrim(float alpha = 0.72f) =>
        new() { BgColor = new Color(0.02f, 0.06f, 0.10f, alpha) };

    private static StyleBoxTexture ButtonBase(Color bg, Color border, int seed) =>
        PaintedStyle(bg, border, 14, seed);

    public static StyleBoxTexture PillNormal() =>
        ButtonBase(new Color(.64f,.84f,.68f,.98f), new Color(.07f,.25f,.24f,1f), 103);

    public static StyleBoxTexture PillHover() =>
        ButtonBase(new Color(.76f,.91f,.72f,1f), new Color(.12f,.34f,.28f,1f), 107);

    public static StyleBoxTexture PillPressed() =>
        ButtonBase(new Color(.50f,.73f,.58f,1f), new Color(.04f,.19f,.18f,1f), 109);

    public static StyleBoxTexture PillDisabled() =>
        ButtonBase(new Color(.16f,.25f,.24f,.42f), new Color(.35f,.48f,.43f,.42f), 113);

    private static StyleBoxTexture SecondaryNormal() =>
        ButtonBase(new Color(.055f,.14f,.15f,.94f), new Color(.43f,.67f,.57f,.92f), 127);

    public static Button StyleButton(Button btn, int fontSize = FontBody, bool primary = true)
    {
        btn.CustomMinimumSize = new Vector2(0, 64);
        btn.AddThemeStyleboxOverride("normal", primary ? PillNormal() : SecondaryNormal());
        btn.AddThemeStyleboxOverride("hover", PillHover());
        btn.AddThemeStyleboxOverride("pressed", PillPressed());
        btn.AddThemeStyleboxOverride("focus", PillHover());
        btn.AddThemeStyleboxOverride("disabled", PillDisabled());
        btn.AddThemeColorOverride("font_color", primary ? new Color(.045f,.18f,.18f) : Ink);
        btn.AddThemeColorOverride("font_hover_color", new Color(.035f,.15f,.15f));
        btn.AddThemeColorOverride("font_pressed_color", new Color(.03f,.12f,.13f));
        btn.AddThemeColorOverride("font_focus_color", new Color(.035f,.15f,.15f));
        btn.AddThemeColorOverride("font_disabled_color", InkFaint);
        ApplyStrongFont(btn, fontSize);
        UiFx.AnimateButton(btn);
        return btn;
    }

    public static void ApplyFont(Control c, int size)
    {
        c.AddThemeFontOverride("font", BodyFont);
        c.AddThemeFontSizeOverride("font_size", size);
    }

    public static void ApplyStrongFont(Control c, int size)
    {
        c.AddThemeFontOverride("font", StrongFont);
        c.AddThemeFontSizeOverride("font_size", size);
    }

    public static void ApplyDisplayFont(Control c, int size)
    {
        Font display = DisplayFont != null ? DisplayFont : StrongFont;
        c.AddThemeFontOverride("font", display);
        c.AddThemeFontSizeOverride("font_size", size);
    }

    public static Label MakeLabel(string text, int size, Color color, bool wrap = false)
    {
        var l = new Label { Text = text };
        if (wrap)
        {
            l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            l.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeConstantOverride("line_spacing", Mathf.RoundToInt(size * 0.38f));
        ApplyFont(l, size);
        return l;
    }

    public static Label MakeStrongLabel(string text, int size, Color color, bool wrap = false)
    {
        var label = MakeLabel(text, size, color, wrap);
        ApplyStrongFont(label, size);
        return label;
    }

    public static Label MakeDisplayLabel(string text, int size, Color color, bool wrap = false)
    {
        var label = MakeLabel(text, size, color, wrap);
        ApplyDisplayFont(label, size);
        return label;
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

    public static StyleBoxTexture BrushBar(Color color, bool track = false)
    {
        string key = $"{color.ToHtml()}_{track}";
        if (!BrushTextures.TryGetValue(key, out var texture))
        {
            const int width = 96, height = 16;
            var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
            for (int x = 0; x < width; x++)
            {
                float top = 2.5f + Wobble(x, track ? 211 : 223) * .45f;
                float bottom = 13f + Wobble(x, track ? 229 : 233) * .38f;
                for (int y = 0; y < height; y++)
                {
                    if (y < top || y > bottom) { image.SetPixel(x,y,Colors.Transparent); continue; }
                    float alpha = color.A * (.72f + Hash01(x,y,241) * .28f);
                    float grain = .90f + Hash01(x/3,y/2,251) * .14f;
                    image.SetPixel(x,y,new Color(color.R*grain,color.G*grain,color.B*grain,alpha));
                }
            }
            texture = ImageTexture.CreateFromImage(image);
            BrushTextures[key] = texture;
        }
        var style = new StyleBoxTexture
        {
            Texture = texture, TextureMarginLeft = 8, TextureMarginRight = 8,
            TextureMarginTop = 5, TextureMarginBottom = 5,
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch,
        };
        style.SetContentMarginAll(0);
        return style;
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
        theme.DefaultFont = BodyFont;
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

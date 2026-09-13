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
    private static Font? _bodyFont;
    private static Font? _strongFont;
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

    // --- Text face ---------------------------------------------------------------
    // Bundled with the game rather than borrowed from the OS: a system face renders
    // differently (or not at all) on the machine the game is sent to, and Godot's
    // SystemFont could not even pick a weight out of most macOS families. A variable
    // font serves both weights from one file; a static family uses two files.
    //
    // Two faces, children's-game style: a loud, chunky headline face for everything
    // that names or directs (objective, names, speaker, labels, buttons, key caps),
    // and a heavy but calm text face for everything that is read at length.
    private const string HeadlineFacePath = "res://assets/fonts/Gluten-VF.ttf";
    private const int HeadlineWeight = 800;  // ignored by single-weight display faces
    private const string BodyFacePath = "res://assets/fonts/Grandstander-VF.ttf";
    private const int BodyWeight = 600;      // heavy on purpose: thin text vanished on the art

    /// Dialogue, prose, HUD data.
    public static Font BodyFont => _bodyFont ??= Face(BodyFacePath, BodyWeight);
    /// Objective, names, labels, headings, buttons.
    public static Font StrongFont => _strongFont ??= Face(HeadlineFacePath, HeadlineWeight);

    /// One face at one weight, with optional tracking and tabular figures. Built
    /// straight off the font file so weight, spacing and features compose in a
    /// single FontVariation instead of stacking variations on variations.
    private static Font Face(string path, int weight, int spacing = 0, bool tabular = false)
    {
        var file = ResourceLoader.Exists(path) ? GD.Load<FontFile>(path) : null;
        var ts = TextServerManager.GetPrimaryInterface();
        var face = new FontVariation { BaseFont = file ?? (Font)MakeSystemFont(weight) };
        int wght = (int)ts.NameToTag("wght");
        if (file != null && file.GetSupportedVariationList().ContainsKey(wght))
            face.VariationOpentype = new Godot.Collections.Dictionary { { wght, weight } };
        if (spacing != 0) face.SpacingGlyph = spacing;
        if (tabular)
            face.OpentypeFeatures = new Godot.Collections.Dictionary { { (int)ts.NameToTag("tnum"), 1 } };
        return face;
    }

    // --- Type scale -------------------------------------------------------------
    // Roughly 1.2 between steps and never closer than 3px, so no two roles can be
    // mistaken for each other on a compressed capture. Size is only one of three
    // levers: every role also fixes a weight and an ink (see TypeRole below).
    public const int SizeDisplay = 96;   // title logo, chapter cards
    public const int SizeHeading = 48;   // panel titles
    public const int SizePrimary = 38;   // the one thing to do right now
    public const int SizeSpeaker = 30;   // who is talking — leads the line it introduces
    public const int SizeName = 28;      // who something is
    public const int SizeBody = 26;      // dialogue and prose
    public const int SizeMeta = 21;      // what it is / secondary numbers
    public const int SizeEyebrow = 17;   // tracked uppercase kicker labels

    /// Last resort if the bundled face is missing. Most macOS families only expose one
    /// face to SystemFont ("Avenir Next" returned Bold for every weight), so this
    /// sticks to families verified to resolve both weights.
    private static SystemFont MakeSystemFont(int weight) => new()
    {
        FontNames = new[] { "SF Pro Text", "Arial" },
        FontWeight = weight,
        AllowSystemFallback = true,
        MultichannelSignedDistanceField = true,
    };

    // --- Spacing spec -------------------------------------------------------------
    // One 8-point spec for every box that holds text; nothing else sets padding.
    // The painted panel edge inks roughly 10px into its box, so these are measured
    // from the outside edge and already include that allowance: with the old 12px
    // insets the text sat about 2px off the ink line.
    public const int PadSurfaceX = 32;   // every panel that holds text in play
    public const int PadSurfaceY = 32;   // = X: the painted frame's edge sits ~7px inside the rect, so 24 read as ~16
    public const int PadScreen = 48;     // full-screen panels: journal, results, tutorial
    public const int PadPillX = 16;      // key prompts
    public const int PadPillY = 8;
    public const int PadKeyX = 8;        // the key cap inside a prompt
    public const int PadKeyY = 4;
    public const int GapPair = 8;        // kicker → value, name → subtitle, line → line
    public const int GapBlock = 16;      // between blocks inside one surface
    public const int GapSection = 24;    // between sections of a full-screen panel
    public const int SafeArea = 72;      // screen edge → HUD surfaces

    /// Outline on text drawn straight over the painting (names, distances).
    public const int OutlineWorld = 6;

    public static readonly Color Ink = new(0.88f, 0.96f, 0.89f);
    public static readonly Color InkDim = new(0.66f, 0.82f, 0.76f);
    public static readonly Color InkFaint = new(0.54f, 0.70f, 0.66f, 0.76f);
    /// Dark ink for text on light fills (key caps, primary buttons).
    public static readonly Color InkOnLight = new(0.035f, 0.14f, 0.15f);
    /// Outline behind world text; the same charcoal as the backdrop line art.
    public static readonly Color OutlineInk = new(0.01f, 0.05f, 0.07f, 0.94f);
    public static readonly Color Accent = new(0.58f, 0.91f, 0.76f);
    /// "Go here / you are here": the one warm colour in a teal world. The edge pointer,
    /// the current step pip and examinable glints all use it and nothing else does.
    public static readonly Color Guide = new(1.00f, 0.78f, 0.22f);
    /// Primary button fill and its ink edge; the world target marker reuses them.
    public static readonly Color PillFill = new(.64f, .84f, .68f, .98f);
    public static readonly Color PillEdge = new(.07f, .25f, .24f);
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

    public static StyleBoxTexture OverlayPanel(int radius = 12, int pad = PadScreen) =>
        PaintedStyle(GlassBgDeep, new Color(.62f,.82f,.69f,.96f), pad, 71 + radius);

    public static StyleBoxFlat Scrim(float alpha = 0.72f) =>
        new() { BgColor = new Color(0.02f, 0.06f, 0.10f, alpha) };

    private static StyleBoxTexture ButtonBase(Color bg, Color border, int seed) =>
        PaintedStyle(bg, border, 14, seed);

    public static StyleBoxTexture PillNormal() =>
        ButtonBase(PillFill, PillEdge, 103);

    public static StyleBoxTexture PillHover() =>
        ButtonBase(new Color(.76f,.91f,.72f,1f), new Color(.12f,.34f,.28f,1f), 107);

    public static StyleBoxTexture PillPressed() =>
        ButtonBase(new Color(.50f,.73f,.58f,1f), new Color(.04f,.19f,.18f,1f), 109);

    public static StyleBoxTexture PillDisabled() =>
        ButtonBase(new Color(.16f,.25f,.24f,.42f), new Color(.35f,.48f,.43f,.42f), 113);

    private static StyleBoxTexture SecondaryNormal() =>
        ButtonBase(new Color(.055f,.14f,.15f,.94f), new Color(.43f,.67f,.57f,.92f), 127);

    public const int ButtonHeight = 64;

    /// Every button: one height, one text size, one text face. `primary` only swaps
    /// the light fill for the dark one.
    public static Button StyleButton(Button btn, bool primary = true)
    {
        btn.CustomMinimumSize = new Vector2(0, ButtonHeight);
        btn.AddThemeStyleboxOverride("normal", primary ? PillNormal() : SecondaryNormal());
        btn.AddThemeStyleboxOverride("hover", PillHover());
        btn.AddThemeStyleboxOverride("pressed", PillPressed());
        btn.AddThemeStyleboxOverride("focus", PillHover());
        btn.AddThemeStyleboxOverride("disabled", PillDisabled());
        btn.AddThemeColorOverride("font_color", primary ? InkOnLight : Ink);
        btn.AddThemeColorOverride("font_hover_color", InkOnLight);
        btn.AddThemeColorOverride("font_pressed_color", InkOnLight);
        btn.AddThemeColorOverride("font_focus_color", InkOnLight);
        btn.AddThemeColorOverride("font_disabled_color", InkFaint);
        btn.AddThemeFontOverride("font", StrongFont);
        btn.AddThemeFontSizeOverride("font_size", SizeBody);
        UiFx.AnimateButton(btn);
        return btn;
    }

    // --- Semantic type roles -----------------------------------------------------
    // Call sites pick a role, never a raw size/weight/colour. Two weights (Body 450,
    // Strong 650) plus the hand-drawn display face are the whole palette; hierarchy
    // comes from combining size, weight and ink per role, not from any one alone.

    private static Font? _eyebrowFont;
    private static Font? _numeralFont;

    /// Strong face with open tracking. Label has no letter-spacing theme constant in
    /// Godot 4 (the old "letter_spacing" override was silently ignored), so tracking
    /// has to live on the font itself.
    public static Font EyebrowFont => _eyebrowFont ??= Face(HeadlineFacePath, HeadlineWeight, spacing: 2);

    /// Body face with tabular figures, so changing numbers (HP, counts) keep their
    /// width instead of jittering as digits change.
    public static Font NumeralFont => _numeralFont ??= Face(BodyFacePath, BodyWeight, tabular: true);

    public enum TypeRole
    {
        Display,   // hand-drawn face, the few moments meant to feel authored
        Heading,   // panel / screen titles
        Primary,   // current objective — the brightest, largest HUD text
        Speaker,   // dialogue speaker: a size above the line, headline face, accent ink
        Body,      // dialogue lines, prose, toast messages
        Name,      // world nameplates, zone name, boss name
        Meta,      // subtitles, roles, secondary info
        Numeral,   // HP, counts — tabular
        Eyebrow,   // tracked uppercase kicker above a heading or value
        Hint,      // tertiary: control reminders, fades into the background
    }

    public static Label Role(TypeRole role, string text, bool wrap = false)
    {
        var label = new Label { Text = text };
        if (wrap)
        {
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }
        return Style(label, role);
    }

    /// Apply a role to a label that already exists (one authored in a .tscn).
    public static Label Style(Label label, TypeRole role)
    {
        var (size, font, ink, upper) = role switch
        {
            TypeRole.Display => (SizeDisplay, (Font)(DisplayFont ?? (Font)StrongFont), Accent, false),
            // Screen and panel titles share the display ink, so every title reads alike.
            TypeRole.Heading => (SizeHeading, (Font)StrongFont, Accent, false),
            TypeRole.Primary => (SizePrimary, (Font)StrongFont, Ink, false),
            TypeRole.Speaker => (SizeSpeaker, (Font)StrongFont, Accent, false),
            TypeRole.Body => (SizeBody, (Font)BodyFont, Ink, false),
            TypeRole.Name => (SizeName, (Font)StrongFont, Ink, false),
            TypeRole.Meta => (SizeMeta, (Font)BodyFont, InkDim, false),
            TypeRole.Numeral => (SizeMeta, (Font)NumeralFont, Ink, false),
            TypeRole.Eyebrow => (SizeEyebrow, (Font)EyebrowFont, Accent, true),
            TypeRole.Hint => (SizeEyebrow, (Font)EyebrowFont, InkFaint, true),
            _ => (SizeBody, (Font)BodyFont, Ink, false),
        };

        if (upper) label.Text = label.Text.ToUpperInvariant();
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", ink);
        // Prose breathes more than labels; headings sit tight.
        float leading = role switch
        {
            TypeRole.Body => 0.45f,
            TypeRole.Heading or TypeRole.Display or TypeRole.Primary => 0.18f,
            _ => 0.28f,
        };
        label.AddThemeConstantOverride("line_spacing", Mathf.RoundToInt(size * leading));
        return label;
    }

    /// Text drawn over the painted world: adds a soft dark outline so it survives any
    /// part of the backdrop without a panel behind it.
    public static Label WorldRole(TypeRole role, string text)
    {
        var label = Role(role, text);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeColorOverride("font_outline_color", OutlineInk);
        label.AddThemeConstantOverride("outline_size", OutlineWorld);
        return label;
    }

    /// The one nameplate for anything in the world you can talk to or use: name, an
    /// optional accent subtitle, and the key prompt, stacked under the host at `y`.
    /// Returns the prompt, hidden until the player is in range; the plate is its parent.
    public static Control WorldPlate(Node2D host, float y, string name, string? subtitle, string action)
    {
        const float width = 360f;
        var plate = new VBoxContainer
        {
            Name = "Plate", Position = new Vector2(-width / 2f, y), Size = new Vector2(width, 0),
            ZIndex = 4, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        plate.AddThemeConstantOverride("separation", GapPair);
        plate.AddChild(WorldRole(TypeRole.Name, name));
        if (subtitle != null)
        {
            var sub = WorldRole(TypeRole.Meta, subtitle);
            sub.AddThemeColorOverride("font_color", Accent);
            plate.AddChild(sub);
        }
        var prompt = new CenterContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        prompt.AddChild(KeyPrompt("E", action));
        plate.AddChild(prompt);
        host.AddChild(plate);
        return prompt;
    }

    /// The one interaction prompt used everywhere: a light key-cap carrying the key,
    /// followed by the action in tracked caps. Reads as "press this", not as another
    /// nameplate, which is what a same-style text label could not do.
    public static PanelContainer KeyPrompt(string key, string action)
    {
        var pill = new PanelContainer { Name = "KeyPrompt", MouseFilter = Control.MouseFilterEnum.Ignore };
        var pillStyle = new StyleBoxFlat
        {
            BgColor = GlassBgDeep,
            BorderColor = new Color(Accent, 0.55f),
            CornerRadiusTopLeft = 20, CornerRadiusTopRight = 20,
            CornerRadiusBottomLeft = 20, CornerRadiusBottomRight = 20,
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            ContentMarginLeft = PadPillY, ContentMarginRight = PadPillX,
            ContentMarginTop = PadPillY, ContentMarginBottom = PadPillY,
        };
        pill.AddThemeStyleboxOverride("panel", pillStyle);

        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", GapPair);
        pill.AddChild(row);

        var cap = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var capStyle = new StyleBoxFlat
        {
            BgColor = Ink,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = PadKeyX, ContentMarginRight = PadKeyX,
            ContentMarginTop = PadKeyY, ContentMarginBottom = PadKeyY,
        };
        cap.AddThemeStyleboxOverride("panel", capStyle);
        var keyLabel = Role(TypeRole.Name, key);
        keyLabel.AddThemeFontSizeOverride("font_size", SizeMeta);
        keyLabel.AddThemeColorOverride("font_color", InkOnLight);
        keyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        cap.AddChild(keyLabel);
        row.AddChild(cap);

        var actionLabel = Role(TypeRole.Eyebrow, action);
        actionLabel.AddThemeColorOverride("font_color", Ink);
        actionLabel.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(actionLabel);
        return pill;
    }

    /// A small framed picture used beside HUD text (objective target, player, boss,
    /// element). Same frame as the dialogue portrait, so pictures read as one family.
    public static PanelContainer Thumbnail(float size, out TextureRect image)
    {
        var frame = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        // The painted frame needs ~56px to draw its corners; smaller icons go bare
        // rather than get a squashed edge.
        frame.AddThemeStyleboxOverride("panel",
            size >= 56 ? GlassPanel(8, 0.28f, GapPair) : new StyleBoxEmpty());
        frame.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        image = new TextureRect
        {
            CustomMinimumSize = new Vector2(size, size),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        frame.AddChild(image);
        return frame;
    }

    /// The picture a world node is drawn with: its own sprite, else its first visible
    /// sprite child. Lets the HUD show "who/what" without a lookup table per chapter.
    public static Texture2D? PictureOf(Node? node)
    {
        static Texture2D? Own(Node n) => n switch
        {
            Sprite2D { Texture: not null, Visible: true } s => s.Texture,
            AnimatedSprite2D { SpriteFrames: not null, Visible: true } a
                when a.SpriteFrames.HasAnimation(a.Animation) && a.SpriteFrames.GetFrameCount(a.Animation) > 0
                => a.SpriteFrames.GetFrameTexture(a.Animation, 0),
            _ => null,
        };
        if (node == null || !GodotObject.IsInstanceValid(node)) return null;
        if (Own(node) is { } own) return own;
        foreach (var child in node.GetChildren())
            if (Own(child) is { } found) return found;
        return null;
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
        theme.DefaultFontSize = SizeBody;
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

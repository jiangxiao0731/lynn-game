using Godot;

namespace ShallowSeaDream;

/// res://scripts/TutorialController.cs
/// 3-page lore/controls modal. Final page tweens the jellyfish base→water, then
/// loads res://scenes/game_scene1.tscn (DESIGN_BRIEF §1, §6).
public partial class TutorialController : Control
{
    [Export] public string GameScenePath = "res://scenes/game_scene1.tscn";

    private int _page;
    private const int PageCount = 3;

    private Label _title = null!;
    private Label _body = null!;
    private Button _next = null!;
    private Panel? _stage;
    private Label? _glyph;
    private TextureRect? _jelly;
    private Label? _pageDots;

    private static readonly (string Title, string Body)[] Pages =
    {
        ("Chapter 1 · Tidepool Nursery",
         "The ocean carries memory through its currents.\n\nPollution has settled in the deep and changed the creatures living here. They are not enemies. Restore the flow, and they can recover."),
        ("Controls",
         "Move       ←  ↑  →  ↓\nInteract   E\nElements   1 Water  ·  2 Ice  ·  3 Electric\n\nR Base form    T Restart chapter    Esc Pause"),
        ("Water Element",
         "Guide Water through polluted gaps and wake the lights trapped inside.\n\nStart by finding Granny Lan. Listen to the residents, collect Tidal Shards, and restore the Brood Mother in the deep."),
    };

    public override void _Ready()
    {
        var bg = GetNodeOrNull<ColorRect>("Background");
        if (bg != null) bg.Color = Palette.DeepSea;

        // Reuse the opening-zone painting so the transition from title to play feels
        // continuous. It is deliberately subdued behind the reading column.
        var introTexture = AssetLoader.Texture(AssetLoader.ZoneBackground(NarrativeData.ZoneShallows));
        if (introTexture != null)
        {
            var art = new TextureRect
            {
                Name = "IntroBackgroundArt",
                Texture = introTexture,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                Modulate = new Color(0.42f, 0.56f, 0.61f, 0.40f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            art.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(art);
            MoveChild(art, 1);
        }

        _title = GetNode<Label>("Modal/VBox/PageTitle");
        _body = GetNode<Label>("Modal/VBox/PageBody");
        _next = GetNode<Button>("Modal/VBox/NextButton");
        _stage = GetNodeOrNull<Panel>("JellyfishStage");
        _glyph = GetNodeOrNull<Label>("JellyfishStage/Glyph");

        StyleScreen();

        _next.Pressed += OnNextPressed;
        UiTheme.StyleButton(_next, UiTheme.FontBody);
        ShowPage(0);
    }

    /// Split composition: a generous character field on the left and a compact,
    /// editorial reading column on the right. It reads as an authored intro rather
    /// than a modal stacked on top of another modal.
    private void StyleScreen()
    {
        var modal = GetNodeOrNull<PanelContainer>("Modal");
        if (modal != null)
        {
            modal.AnchorLeft = 0.54f;
            modal.AnchorTop = 0.16f;
            modal.AnchorRight = 0.93f;
            modal.AnchorBottom = 0.84f;
            modal.OffsetLeft = modal.OffsetTop = modal.OffsetRight = modal.OffsetBottom = 0;
            modal.AddThemeStyleboxOverride("panel", UiTheme.GlassPanel(radius: 10, bgAlpha: 0.78f, pad: 48));
        }
        var vb = GetNodeOrNull<VBoxContainer>("Modal/VBox");
        if (vb != null)
        {
            vb.AddThemeConstantOverride("separation", UiTheme.Space6);
            vb.Alignment = BoxContainer.AlignmentMode.Begin;
        }

        UiTheme.ApplyFont(_title, UiTheme.FontH1);
        _title.AddThemeColorOverride("font_color", UiTheme.Accent);
        _title.HorizontalAlignment = HorizontalAlignment.Left;

        UiTheme.ApplyFont(_body, UiTheme.FontBody);
        _body.AddThemeColorOverride("font_color", UiTheme.Ink);
        _body.AddThemeConstantOverride("line_spacing", 14);
        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _body.HorizontalAlignment = HorizontalAlignment.Left;
        _body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _body.CustomMinimumSize = new Vector2(0, 260);

        // Page indicator dots under the body.
        _pageDots = UiTheme.MakeLabel("", UiTheme.FontTiny, UiTheme.InkDim);
        _pageDots.HorizontalAlignment = HorizontalAlignment.Left;
        if (vb != null)
        {
            int nextIdx = _next.GetIndex();
            vb.AddChild(_pageDots);
            vb.MoveChild(_pageDots, nextIdx); // place just above the Next button
        }

        // Character art floats directly in the scene instead of inside a circular card.
        if (_stage != null)
        {
            _stage.AnchorLeft = _stage.AnchorRight = 0.27f;
            _stage.AnchorTop = _stage.AnchorBottom = 0.50f;
            _stage.OffsetLeft = -210;
            _stage.OffsetTop = -210;
            _stage.OffsetRight = 210;
            _stage.OffsetBottom = 210;
            var openStage = new StyleBoxFlat { BgColor = Colors.Transparent };
            _stage.AddThemeStyleboxOverride("panel", openStage);

            var frames = PlaceholderArt.FormFrames(ElementForm.Base);
            var tex = frames.GetFrameTexture("Idle", 0);
            _jelly = new TextureRect
            {
                Name = "JellyPreview",
                Texture = tex,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            };
            _jelly.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _jelly.MouseFilter = MouseFilterEnum.Ignore;
            _stage.AddChild(_jelly);
            StartJellyPulse(_jelly);

            if (_glyph != null) _glyph.Visible = false;
        }
    }

    private static void StartJellyPulse(Control c)
    {
        c.PivotOffset = c.Size / 2f;
        var tw = c.CreateTween().SetLoops();
        tw.TweenProperty(c, "scale", new Vector2(1.06f, 1.06f), 1.8f)
          .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tw.TweenProperty(c, "scale", Vector2.One, 1.8f)
          .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void ShowPage(int page)
    {
        _page = Mathf.Clamp(page, 0, PageCount - 1);
        _title.Text = Pages[_page].Title;
        _body.Text = Pages[_page].Body;
        _next.Text = _page < PageCount - 1 ? "Next" : "Enter Water Form";
        if (_pageDots != null)
        {
            _pageDots.Text = $"{_page + 1:00}  /  {PageCount:00}";
        }
    }

    private void OnNextPressed()
    {
        AudioManager.Instance?.PlaySfx("ui_select");
        if (_page < PageCount - 1)
            ShowPage(_page + 1);
        else
            OnTransformAndStart();
    }

    private void OnTransformAndStart()
    {
        AudioManager.Instance?.PlaySfx("ui_confirm");
        _next.Disabled = true;

        // base→water transform tween on the real jellyfish preview, then load.
        var target = (Control?)_jelly ?? _glyph;
        if (target != null)
        {
            target.PivotOffset = target.Size / 2f;
            var tween = CreateTween();
            tween.TweenProperty(target, "modulate", Palette.ElementWater, 0.6f)
                 .SetTrans(Tween.TransitionType.Sine);
            tween.Parallel().TweenProperty(target, "scale", new Vector2(1.35f, 1.35f), 0.6f)
                 .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
            tween.TweenCallback(Callable.From(() => GetTree().ChangeSceneToFile(GameScenePath)));
        }
        else
        {
            GetTree().ChangeSceneToFile(GameScenePath);
        }
    }
}

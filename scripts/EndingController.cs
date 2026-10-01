using Godot;

namespace ShallowSeaDream;

/// The final campaign coda. This is deliberately a game ending, not a completion
/// report: one restored sea, one hero, one emotional thought, then a clean exit.
public partial class EndingController : Control
{
    private VBoxContainer _story = null!;
    private TextureRect _hero = null!;
    private TextureRect _heroGlow = null!;

    public override void _Ready()
    {
        AudioManager.Instance?.PlayMusic("title_theme");
        BuildBackground();
        BuildEnding();
        CallDeferred(nameof(AnimateEnding));
    }

    private void BuildBackground()
    {
        var baseFill = new ColorRect { Color = new Color(0.012f, 0.07f, 0.10f, 1f) };
        baseFill.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(baseFill);

        var sea = new TextureRect
        {
            Texture = AssetLoader.Texture(AssetLoader.ChapterBackground(3, 2)),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Modulate = new Color(0.78f, 1f, 0.94f, 0.88f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        sea.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(sea);

        // Darker behind the words, open water around Shimmer. The composition reads
        // as one cinematic frame rather than a full-screen modal.
        var wash = new TextureRect
        {
            Texture = PlaceholderArt.HorizontalGradient(
                new Color(0.005f, 0.04f, 0.07f, 0.97f),
                new Color(0.02f, 0.12f, 0.14f, 0.26f)),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        wash.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(wash);

        var floorFade = new ColorRect
        {
            Color = new Color(0.01f, 0.05f, 0.07f, 0.24f),
            AnchorTop = 0.76f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(floorFade);
    }

    private void BuildEnding()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 112);
        margin.AddThemeConstantOverride("margin_right", 112);
        margin.AddThemeConstantOverride("margin_top", 72);
        margin.AddThemeConstantOverride("margin_bottom", 72);
        AddChild(margin);

        var centre = new CenterContainer();
        margin.AddChild(centre);
        var composition = new HBoxContainer { CustomMinimumSize = new Vector2(1500, 760) };
        composition.AddThemeConstantOverride("separation", 72);
        centre.AddChild(composition);

        _story = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            CustomMinimumSize = new Vector2(760, 700),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _story.AddThemeConstantOverride("separation", UiTheme.GapSection);
        composition.AddChild(_story);

        var kicker = UiTheme.Role(UiTheme.TypeRole.Eyebrow, "THE END");
        kicker.HorizontalAlignment = HorizontalAlignment.Left;
        _story.AddChild(kicker);

        var title = UiTheme.Role(UiTheme.TypeRole.Display, "The Sea Glows Again", wrap: true);
        title.AddThemeFontSizeOverride("font_size", 88);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        _story.AddChild(title);

        var lead = UiTheme.Role(UiTheme.TypeRole.Primary,
            "Shimmer followed every lost light home.\nThe currents are moving again.", wrap: true);
        lead.AddThemeFontSizeOverride("font_size", 34);
        lead.HorizontalAlignment = HorizontalAlignment.Left;
        _story.AddChild(lead);

        var coda = UiTheme.Role(UiTheme.TypeRole.Body, "The sea has time to heal.", wrap: true);
        coda.AddThemeColorOverride("font_color", UiTheme.InkDim);
        coda.HorizontalAlignment = HorizontalAlignment.Left;
        _story.AddChild(coda);

        _story.AddChild(new Control { CustomMinimumSize = new Vector2(0, 24) });
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
        buttons.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        _story.AddChild(buttons);

        var titleButton = new Button
        {
            Text = "Return to Title",
            CustomMinimumSize = new Vector2(320, UiTheme.ButtonHeight),
        };
        UiTheme.StyleButton(titleButton);
        titleButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/title.tscn");
        buttons.AddChild(titleButton);

        var replay = new Button
        {
            Text = "Begin Again",
            CustomMinimumSize = new Vector2(260, UiTheme.ButtonHeight),
        };
        UiTheme.StyleButton(replay, primary: false);
        replay.Pressed += () =>
        {
            SaveManager.Instance?.Reset();
            MemoryLog.Instance?.Reset();
            GetTree().ChangeSceneToFile("res://scenes/tutorial.tscn");
        };
        buttons.AddChild(replay);
        titleButton.GrabFocus();

        var heroStage = new Control
        {
            CustomMinimumSize = new Vector2(600, 720),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        composition.AddChild(heroStage);

        _heroGlow = new TextureRect
        {
            Texture = PlaceholderArt.SoftGlow(new Color(0.54f, 0.94f, 0.90f, 0.72f), 256),
            Position = new Vector2(30, 70),
            Size = new Vector2(560, 560),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        heroStage.AddChild(_heroGlow);

        _hero = new TextureRect
        {
            Texture = AssetLoader.Texture(AssetLoader.FormSheet(ElementForm.Water)),
            Position = new Vector2(54, 72),
            Size = new Vector2(520, 520),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        heroStage.AddChild(_hero);

        var finalLine = UiTheme.WorldRole(UiTheme.TypeRole.Meta, "SHIMMER · KEEPER OF THE LAST LIGHT");
        finalLine.Position = new Vector2(70, 610);
        finalLine.Size = new Vector2(500, 48);
        heroStage.AddChild(finalLine);
    }

    private void AnimateEnding()
    {
        _story.Modulate = Colors.Transparent;
        Vector2 storyRest = _story.Position;
        _story.Position = storyRest + new Vector2(-24f, 0f);

        _hero.Modulate = Colors.Transparent;
        _heroGlow.Modulate = Colors.Transparent;
        _hero.PivotOffset = _hero.Size * 0.5f;
        _hero.Scale = Vector2.One * 0.94f;

        var reveal = CreateTween();
        reveal.TweenProperty(_story, "modulate", Colors.White, 0.65f)
            .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
        reveal.Parallel().TweenProperty(_story, "position", storyRest, 0.65f)
            .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
        reveal.TweenProperty(_heroGlow, "modulate", Colors.White, 0.55f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        reveal.Parallel().TweenProperty(_hero, "modulate", Colors.White, 0.48f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        reveal.Parallel().TweenProperty(_hero, "scale", Vector2.One, 0.68f)
            .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
        reveal.TweenCallback(Callable.From(StartHeroFloat));
    }

    private void StartHeroFloat()
    {
        Vector2 rest = _hero.Position;
        var floatTween = _hero.CreateTween().SetLoops();
        floatTween.TweenProperty(_hero, "position:y", rest.Y - 12f, 2.4f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        floatTween.TweenProperty(_hero, "position:y", rest.Y + 8f, 2.8f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        var glowTween = _heroGlow.CreateTween().SetLoops();
        glowTween.TweenProperty(_heroGlow, "modulate:a", 0.58f, 2.2f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        glowTween.TweenProperty(_heroGlow, "modulate:a", 0.92f, 2.2f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}

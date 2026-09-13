using Godot;

namespace ShallowSeaDream;

/// res://scripts/TitleController.cs
/// Title screen: Begin (潜入海洋 / 继续游戏·开始游戏) and Exit (返回岸上). Save-aware
/// label; plays title_theme.ogg; fades to res://scenes/tutorial.tscn.
public partial class TitleController : Control
{
    [Export] public string TutorialScenePath = "res://scenes/tutorial.tscn";

    public override void _Ready()
    {
        AudioManager.Instance?.PlayMusic("title_theme");

        // Deep-sea base wash (under the optional cover image).
        var baseBg = GetNodeOrNull<ColorRect>("Background");
        if (baseBg != null) baseBg.Color = Palette.DeepSea;

        // Optional generated title background — graceful fallback to the ColorRect.
        var bgTex = AssetLoader.Texture(AssetLoader.TitleBackground);
        if (bgTex != null)
        {
            var tr = new TextureRect
            {
                Name = "TitleBgImage",
                Texture = bgTex,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            };
            tr.SetAnchorsPreset(LayoutPreset.FullRect);
            tr.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(tr);
            MoveChild(tr, 1); // above the ColorRect, below the menu

            // A quiet wash plus a stronger left reading field. The right side of the
            // painting stays visible, while the menu has dependable contrast.
            var scrim = new ColorRect { Name = "Scrim", Color = new Color(0.03f, 0.09f, 0.14f, 0.24f) };
            scrim.SetAnchorsPreset(LayoutPreset.FullRect);
            scrim.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(scrim);
            MoveChild(scrim, 2);

            var readingField = new ColorRect
            {
                Name = "ReadingField",
                Color = new Color(0.018f, 0.065f, 0.09f, 0.16f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            readingField.AnchorRight = 0.50f;
            readingField.AnchorBottom = 1f;
            AddChild(readingField);
            MoveChild(readingField, 3);
        }

        // A single irregular watercolor folio replaces the old full-height glass
        // reading field. It gives the title a memorable illustrated-book cover.
        var folio = new Panel { Name = "TitleFolio", MouseFilter = MouseFilterEnum.Ignore };
        folio.AnchorLeft = .045f; folio.AnchorTop = .075f;
        folio.AnchorRight = .47f; folio.AnchorBottom = .925f;
        folio.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 7, pad: 0));
        AddChild(folio);
        var center = GetNodeOrNull<Control>("Center");
        if (center != null) MoveChild(folio, center.GetIndex());

        BuildMenu();
    }

    private void BuildMenu()
    {
        var menu = GetNodeOrNull<VBoxContainer>("Center/Menu");
        if (menu == null) return;

        // Clear the bare .tscn placeholder children; rebuild styled to the central theme.
        foreach (var c in menu.GetChildren()) c.QueueFree();
        menu.AddThemeConstantOverride("separation", UiTheme.Space4);
        menu.Alignment = BoxContainer.AlignmentMode.Begin;

        var saved = SaveManager.Instance?.LoadState();
        int savedLevel = saved?.CurrentLevel ?? 1;
        string chapterText = savedLevel == 2 ? "CHAPTER 2  ·  FROSTBOUND TRENCH"
            : savedLevel == 3 ? "CHAPTER 3  ·  THE SILENT LIGHTHOUSE" : "CHAPTER 1  ·  TIDEPOOL NURSERY";
        var chapter = UiTheme.Role(UiTheme.TypeRole.Eyebrow, chapterText);
        menu.AddChild(chapter);

        // Editorial, left-aligned title lockup. One restrained glow belongs to the
        // title only; buttons and supporting text remain quiet.
        var title = UiTheme.Role(UiTheme.TypeRole.Display, "Shallow Sea\nDream");
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.CustomMinimumSize = new Vector2(620, 184);
        title.AddThemeConstantOverride("line_spacing", -8);
        title.AddThemeColorOverride("font_outline_color",
            new Color(Palette.CoastalCyan.R, Palette.CoastalCyan.G, Palette.CoastalCyan.B, 0.18f));
        title.AddThemeConstantOverride("outline_size", 8);
        menu.AddChild(title);
        StartTitlePulse(title);

        var subtitle = UiTheme.Role(UiTheme.TypeRole.Eyebrow, "An ocean restoration story");
        subtitle.AddThemeFontSizeOverride("font_size", UiTheme.SizeMeta);
        subtitle.AddThemeColorOverride("font_color", UiTheme.InkDim);
        subtitle.HorizontalAlignment = HorizontalAlignment.Left;
        menu.AddChild(subtitle);

        var premise = UiTheme.Role(UiTheme.TypeRole.Body, "Protect the last light. Help a polluted ocean recover.", wrap: true);
        premise.CustomMinimumSize = new Vector2(620, 64);
        menu.AddChild(premise);
        menu.AddChild(new Control { CustomMinimumSize = new Vector2(0, UiTheme.Space8) });

        var actions = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
        actions.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        actions.AddThemeConstantOverride("separation", UiTheme.Space3);
        menu.AddChild(actions);

        bool hasSave = SaveManager.Instance?.HasSave() ?? false;

        var begin = new Button { Text = hasSave ? "Continue Journey" : "Start Game", Name = "BeginButton" };
        begin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UiTheme.StyleButton(begin, UiTheme.FontBody);
        begin.Pressed += OnBeginPressed;
        actions.AddChild(begin);

        if (hasSave)
        {
            var newGame = new Button { Text = "Start New Game", Name = "NewGameButton" };
            newGame.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            UiTheme.StyleButton(newGame, UiTheme.FontBody, primary: false);
            newGame.Pressed += OnNewGamePressed;
            actions.AddChild(newGame);
        }

        var exit = new Button { Text = "Exit Game", Name = "ExitButton" };
        exit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UiTheme.StyleButton(exit, UiTheme.FontBody, primary: false);
        exit.Pressed += OnExitPressed;
        actions.AddChild(exit);

        begin.GrabFocus();
    }

    /// Slow bioluminescent breathing on the hero title.
    private static void StartTitlePulse(Label title)
    {
        var tw = title.CreateTween().SetLoops();
        tw.TweenProperty(title, "modulate", new Color(1.18f, 1.18f, 1.18f, 1f), 2.4f)
          .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tw.TweenProperty(title, "modulate", Colors.White, 2.4f)
          .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void OnBeginPressed()
    {
        AudioManager.Instance?.PlaySfx("ui_confirm");
        var state = SaveManager.Instance?.LoadState();
        string path = state?.CurrentLevel switch
        {
            2 => "res://scenes/game_scene2.tscn",
            3 => "res://scenes/game_scene3.tscn",
            _ => TutorialScenePath,
        };
        GetTree().ChangeSceneToFile(path);
    }

    private void OnNewGamePressed()
    {
        SaveManager.Instance?.Reset();
        OnBeginPressed();
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}

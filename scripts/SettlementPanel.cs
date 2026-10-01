using Godot;

namespace ShallowSeaDream;

/// Visual chapter result: a pollution → recovery comparison, illustrated trophies,
/// and one short reflection. The long report-style paragraph is deliberately demoted
/// so completing a chapter feels like a transformation rather than a text modal.
public partial class SettlementPanel : Control
{
    [Export] public string TitleScenePath = "res://scenes/title.tscn";
    [Export] public string NextScenePath = "";

    private Label? _summary;
    private PanelContainer? _box;
    private Button? _continue;

    public override void _Ready()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;

        var dim = GetNodeOrNull<ColorRect>("Dim");
        if (dim != null) dim.Color = new Color(0.01f, 0.045f, 0.07f, 0.91f);

        _box = GetNodeOrNull<PanelContainer>("Box");
        var layout = GetNodeOrNull<VBoxContainer>("Box/VBox");
        if (_box == null || layout == null) return;
        _box.CustomMinimumSize = new Vector2(1120, 820);
        _box.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 24, pad: 40));

        foreach (var child in layout.GetChildren())
        {
            layout.RemoveChild(child);
            child.Free();
        }
        layout.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        BuildResult(layout, ChapterRuntime.CurrentChapter);
    }

    private void BuildResult(VBoxContainer layout, int chapter)
    {
        var kicker = UiTheme.Role(UiTheme.TypeRole.Eyebrow, $"CHAPTER {chapter} · RESTORATION COMPLETE");
        kicker.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(kicker);

        var title = UiTheme.Role(UiTheme.TypeRole.Heading, ChapterTitle(chapter));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(title);

        var compare = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        compare.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        layout.AddChild(compare);
        var before = DialoguePicture.ResultBeforeForChapter(chapter);
        var after = DialoguePicture.ResultAfterForChapter(chapter);
        compare.AddChild(PictureCard("BEFORE", before.Path,
            CaptionWithCredit(before, BeforeCaption(chapter)), new Color(Palette.ArenaTint(chapter), 0.34f)));
        compare.AddChild(PictureCard("AFTER", after.Path,
            CaptionWithCredit(after, AfterCaption(chapter)), new Color(UiTheme.Accent, 0.22f)));

        var change = UiTheme.Role(UiTheme.TypeRole.Primary, TransformationLine(chapter), wrap: true);
        change.HorizontalAlignment = HorizontalAlignment.Center;
        change.CustomMinimumSize = new Vector2(0, 58);
        layout.AddChild(change);

        var metrics = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        metrics.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        layout.AddChild(metrics);
        metrics.AddChild(MetricCard(PollutionIcon(chapter), "POLLUTION LIFTED", CollectedFor(chapter).ToString()));
        metrics.AddChild(MetricCard(AssetLoader.ChapterBossPortrait(chapter), "MONSTER", "CLEANED UP"));
        metrics.AddChild(MetricCard(AssetLoader.MemoryIcon, "SEA CHANGE", SeaChange(chapter)));

        _summary = UiTheme.Role(UiTheme.TypeRole.Meta, "", wrap: true);
        _summary.HorizontalAlignment = HorizontalAlignment.Center;
        _summary.CustomMinimumSize = new Vector2(0, 74);
        layout.AddChild(_summary);

        _continue = new Button
        {
            Name = "ContinueButton",
            Text = string.IsNullOrEmpty(NextScenePath)
                ? "Return to Title"
                : chapter == 1 ? "Enter Frozen Trench"
                : chapter == 2 ? "Go to the Silent Lighthouse"
                : "See What the Sea Remembers",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        UiTheme.StyleButton(_continue);
        _continue.Pressed += OnContinuePressed;
        layout.AddChild(_continue);
    }

    private static Control PictureCard(string label, string path, string caption, Color wash)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", UiTheme.GlassPanel(12, 0.58f, UiTheme.GapBlock));
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", UiTheme.GapPair);
        card.AddChild(stack);
        var tag = UiTheme.Role(UiTheme.TypeRole.Eyebrow, label);
        tag.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(tag);
        var frame = new PanelContainer { CustomMinimumSize = new Vector2(480, 228) };
        var frameStyle = new StyleBoxFlat
        {
            BgColor = wash,
            BorderColor = new Color(UiTheme.Accent, 0.42f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
        };
        frame.AddThemeStyleboxOverride("panel", frameStyle);
        stack.AddChild(frame);
        var image = new TextureRect
        {
            Texture = AssetLoader.Texture(path),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Modulate = label == "BEFORE" ? new Color(0.78f, 0.82f, 0.78f, 0.94f) : Colors.White,
        };
        image.SetAnchorsPreset(LayoutPreset.FullRect);
        image.MouseFilter = MouseFilterEnum.Ignore;
        frame.AddChild(image);
        var text = UiTheme.Role(UiTheme.TypeRole.Meta, caption, wrap: true);
        text.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(text);
        return card;
    }

    private static string CaptionWithCredit(DialoguePicture picture, string note)
        => $"{picture.Caption}\n{note} · {picture.Credit}";

    private static Control MetricCard(string texturePath, string label, string value)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", UiTheme.GlassPanel(10, 0.34f, UiTheme.GapBlock));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        card.AddChild(row);
        var thumb = UiTheme.Thumbnail(72, out var image);
        image.Texture = AssetLoader.Texture(texturePath);
        row.AddChild(thumb);
        var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", UiTheme.GapPair);
        text.AddChild(UiTheme.Role(UiTheme.TypeRole.Eyebrow, label));
        text.AddChild(UiTheme.Role(UiTheme.TypeRole.Name, value));
        row.AddChild(text);
        return card;
    }

    public void ShowResult(string summary)
    {
        if (GetParent() is Node parent)
            foreach (var sibling in parent.GetChildren())
                if (sibling != this && sibling is CanvasItem item)
                    item.Visible = false;
        Visible = true;
        if (_summary != null) _summary.Text = summary;
        GetTree().Paused = true;
        AudioManager.Instance?.PlaySfx("ui_confirm");
        if (_box != null)
        {
            _box.PivotOffset = _box.Size * 0.5f;
            _box.Scale = new Vector2(0.94f, 0.94f);
            _box.Modulate = new Color(1f, 1f, 1f, 0f);
            var reveal = CreateTween();
            reveal.SetPauseMode(Tween.TweenPauseMode.Process).SetParallel();
            reveal.TweenProperty(_box, "scale", Vector2.One, 0.36f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            reveal.TweenProperty(_box, "modulate:a", 1f, 0.24f);
        }
        _continue?.GrabFocus();
    }

    private void OnContinuePressed()
    {
        GetTree().Paused = false;
        AudioManager.Instance?.PlaySfx("ui_confirm");
        if (NextScenePath.EndsWith("game_scene2.tscn")) SaveManager.Instance?.SaveCampaignBoundary(2);
        else if (NextScenePath.EndsWith("game_scene3.tscn")) SaveManager.Instance?.SaveCampaignBoundary(3);
        GetTree().ChangeSceneToFile(string.IsNullOrEmpty(NextScenePath) ? TitleScenePath : NextScenePath);
    }

    private static int CollectedFor(int chapter) => MemoryLog.Instance == null ? 0 : chapter switch
    {
        2 => MemoryLog.Instance.IcePollutionCollected,
        3 => MemoryLog.Instance.OilHeatCollected,
        _ => MemoryLog.Instance.WaterCollected,
    };

    private static string PollutionIcon(int chapter) => chapter switch
    {
        2 => AssetLoader.NpcPortrait("ch2_gascloud"),
        3 => AssetLoader.NpcPortrait("ch3_slickblob"),
        _ => AssetLoader.ChapterElement(1),
    };

    private static string ChapterTitle(int c) => c switch
    {
        2 => "Frozen Trench is Flowing Again",
        3 => "Safe Lights Have Returned",
        _ => "Area Restored",
    };
    private static string BeforeCaption(int c) => c switch
    {
        2 => "Runoff fed blooms; decay pulled oxygen from the trench.",
        3 => "Oil and trapped heat dimmed coral and silenced the grid.",
        _ => "Plastic, ghost gear and waste buried the nursery's light.",
    };
    private static string AfterCaption(int c) => c switch
    {
        2 => "Flow Switches move oxygenated water through the eggs again.",
        3 => "The safe circuit cools the reef and guides the shoal home.",
        _ => "Open currents carry light across the nursery again.",
    };
    private static string TransformationLine(int c) => c switch
    {
        2 => "stagnant water  →  moving oxygen",
        3 => "waste heat  →  guided energy",
        _ => "buried light  →  living current",
    };
    private static string SeaChange(int c) => c switch
    {
        2 => "OXYGEN",
        3 => "COOL LIGHT",
        _ => "OPEN TIDE",
    };
}

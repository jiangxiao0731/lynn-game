using Godot;

namespace ShallowSeaDream;

/// Campaign epilogue after all three seas are restored. It turns the journal and
/// collection counters into one visual account of what changed—and what still has to
/// change on land—before returning the player to the title.
public partial class EndingController : Control
{
    public override void _Ready()
    {
        AudioManager.Instance?.PlayMusic("title_theme");
        BuildBackground();
        BuildStory();
    }

    private void BuildBackground()
    {
        var baseFill = new ColorRect { Color = new Color(0.015f, 0.08f, 0.12f, 1f) };
        baseFill.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(baseFill);

        var sea = new TextureRect
        {
            Texture = AssetLoader.Texture(AssetLoader.ChapterBackground(3, 2)),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Modulate = new Color(0.72f, 0.92f, 0.88f, 0.82f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        sea.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(sea);

        var wash = new TextureRect
        {
            Texture = PlaceholderArt.HorizontalGradient(
                new Color(0.01f, 0.05f, 0.08f, 0.94f),
                new Color(0.02f, 0.15f, 0.16f, 0.46f)),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        wash.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(wash);
    }

    private void BuildStory()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 96);
        margin.AddThemeConstantOverride("margin_right", 96);
        margin.AddThemeConstantOverride("margin_top", 36);
        margin.AddThemeConstantOverride("margin_bottom", 36);
        AddChild(margin);

        var main = new VBoxContainer();
        main.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        margin.AddChild(main);

        var title = UiTheme.Role(UiTheme.TypeRole.Display, "The Sea Remembers the Light");
        title.AddThemeFontSizeOverride("font_size", 60);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        main.AddChild(title);
        var lead = UiTheme.Role(UiTheme.TypeRole.Body,
            "Three damaged waters are moving again. The change is visible—but recovery begins upstream, with the waste and heat we choose not to send into the sea.", wrap: true);
        lead.HorizontalAlignment = HorizontalAlignment.Center;
        lead.CustomMinimumSize = new Vector2(0, 60);
        main.AddChild(lead);

        var chapters = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        chapters.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        main.AddChild(chapters);
        chapters.AddChild(ChapterCard(1, "TIDEPOOL NURSERY", Count(1), "plastic cleared", "light returned"));
        chapters.AddChild(ChapterCard(2, "FROZEN TRENCH", Count(2), "chemical waste contained", "oxygen moving"));
        chapters.AddChild(ChapterCard(3, "THE OLD LIGHTHOUSE", Count(3), "oil cleaned", "safe lights returned"));

        var total = new PanelContainer();
        total.AddThemeStyleboxOverride("panel", UiTheme.GlassPanel(16, 0.72f, UiTheme.GapBlock));
        main.AddChild(total);
        var totalRow = new HBoxContainer();
        totalRow.AddThemeConstantOverride("separation", UiTheme.GapSection);
        total.AddChild(totalRow);
        totalRow.AddChild(BigStat((MemoryLog.Instance?.TotalPollutionCollected ?? 0).ToString(), "POLLUTION OBJECTS CLEANED"));
        totalRow.AddChild(BigStat((MemoryLog.Instance?.Conversations.Count ?? 0).ToString(), "VOICES HEARD"));
        totalRow.AddChild(BigStat((MemoryLog.Instance?.Notes.Count ?? 0).ToString(), "STORIES FOUND"));
        totalRow.AddChild(BigStat((MemoryLog.Instance?.GuardiansRestored ?? 0).ToString(), "MONSTERS CLEANED UP"));

        var closing = UiTheme.Role(UiTheme.TypeRole.Primary,
            "Protecting the ocean means stopping pollution at its source, not only cleaning it up later.", wrap: true);
        closing.HorizontalAlignment = HorizontalAlignment.Center;
        closing.CustomMinimumSize = new Vector2(0, 70);
        main.AddChild(closing);

        var coda = UiTheme.Role(UiTheme.TypeRole.Meta,
            "Cleanup buys habitats time. Cleaner production, less disposable waste, safer runoff and lower emissions are what let that healing last.", wrap: true);
        coda.HorizontalAlignment = HorizontalAlignment.Center;
        main.AddChild(coda);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        main.AddChild(buttons);
        var replay = new Button { Text = "Begin Again", CustomMinimumSize = new Vector2(300, UiTheme.ButtonHeight) };
        UiTheme.StyleButton(replay, primary: false);
        replay.Pressed += () =>
        {
            SaveManager.Instance?.Reset();
            MemoryLog.Instance?.Reset();
            GetTree().ChangeSceneToFile("res://scenes/tutorial.tscn");
        };
        buttons.AddChild(replay);
        var titleButton = new Button { Text = "Return to Title", CustomMinimumSize = new Vector2(360, UiTheme.ButtonHeight) };
        UiTheme.StyleButton(titleButton);
        titleButton.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/title.tscn");
        buttons.AddChild(titleButton);
        titleButton.GrabFocus();
    }

    private static Control ChapterCard(int chapter, string title, int count, string itemLabel, string outcome)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", UiTheme.GlassPanel(12, 0.64f, UiTheme.GapBlock));
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", UiTheme.GapPair);
        card.AddChild(stack);
        var eyebrow = UiTheme.Role(UiTheme.TypeRole.Eyebrow, $"CHAPTER {chapter}");
        eyebrow.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(eyebrow);
        var name = UiTheme.Role(UiTheme.TypeRole.Name, title, wrap: true);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(name);
        var image = new TextureRect
        {
            Texture = AssetLoader.Texture(AssetLoader.ChapterBackground(chapter, 2)),
            CustomMinimumSize = new Vector2(0, 118),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        };
        stack.AddChild(image);
        var transform = UiTheme.Role(UiTheme.TypeRole.Primary, $"{count} {itemLabel}\n→ {outcome}", wrap: true);
        transform.AddThemeFontSizeOverride("font_size", 24);
        transform.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(transform);
        return card;
    }

    private static Control BigStat(string value, string label)
    {
        var stack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var number = UiTheme.Role(UiTheme.TypeRole.Heading, value);
        number.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(number);
        var caption = UiTheme.Role(UiTheme.TypeRole.Eyebrow, label, wrap: true);
        caption.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(caption);
        return stack;
    }

    private static int Count(int chapter) => MemoryLog.Instance == null ? 0 : chapter switch
    {
        2 => MemoryLog.Instance.IcePollutionCollected,
        3 => MemoryLog.Instance.OilHeatCollected,
        _ => MemoryLog.Instance.WaterCollected,
    };
}

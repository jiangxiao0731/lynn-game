using Godot;

namespace ShallowSeaDream;

/// res://scripts/LogPanel.cs
/// The 「记忆日志 / 图鉴」 overlay (item 5). Toggled with the toggle_log action (J / Tab).
/// Lists, in four scrollable sections: unlocked 微光潮汐记忆 vignettes, NPC
/// conversations seen, monster codex entries, and 残片笔记 lore notes found. Reads the
/// MemoryLog autoload; pauses the tree while open. Built procedurally (no .tscn).
public partial class LogPanel : CanvasLayer
{
    private Panel _panel = null!;
    private Panel _scrim = null!;
    private VBoxContainer _content = null!;
    public bool IsOpen { get; private set; }

    public override void _Ready()
    {
        Layer = 25;
        ProcessMode = ProcessModeEnum.Always;
        BuildUi();
        _panel.Visible = false;
        _scrim.Visible = false;
    }

    private void BuildUi()
    {
        // Full-screen deep-sea scrim, then a centred frosted glass codex card.
        _scrim = new Panel { Name = "Scrim" };
        _scrim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _scrim.AddThemeStyleboxOverride("panel", UiTheme.Scrim(0.78f));
        AddChild(_scrim);

        _panel = new Panel { Name = "LogCard" };
        _panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _panel.OffsetLeft = 160; _panel.OffsetRight = -160;
        _panel.OffsetTop = 80; _panel.OffsetBottom = -80;
        _panel.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 24, pad: UiTheme.PadScreen));
        AddChild(_panel);

        var outer = new VBoxContainer();
        outer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        outer.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        _panel.AddChild(outer);

        outer.AddChild(UiTheme.Role(UiTheme.TypeRole.Heading, GameStrings.Tr("LOG_TITLE")));
        outer.AddChild(UiTheme.Role(UiTheme.TypeRole.Hint, GameStrings.Tr("LOG_HINT")));
        outer.AddChild(UiTheme.Divider());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        outer.AddChild(scroll);

        _content = new VBoxContainer();
        _content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _content.AddThemeConstantOverride("separation", UiTheme.GapPair);
        scroll.AddChild(_content);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    private void Open()
    {
        Rebuild();
        _scrim.Visible = true;
        _panel.Visible = true;
        IsOpen = true;
        GetTree().Paused = true;
        AudioManager.Instance?.PlaySfx("pause_toggle");
    }

    private void Close()
    {
        _panel.Visible = false;
        _scrim.Visible = false;
        IsOpen = false;
        GetTree().Paused = false;
        AudioManager.Instance?.PlaySfx("pause_toggle");
    }

    private void Rebuild()
    {
        foreach (var c in _content.GetChildren()) c.QueueFree();
        var log = MemoryLog.Instance;

        // --- Memories ---
        AddSection(string.Format(GameStrings.Tr("LOG_SEC_MEMORIES"),
            log?.Memories.Count ?? 0, NarrativeData.MemoryCount));
        if (log != null)
            foreach (var id in log.Memories)
            {
                var m = NarrativeData.FindMemory(id);
                if (m != null) AddEntry(m.Title, string.Join(" ", m.Lines), m.Zone);
            }

        // --- Conversations ---
        AddSection(string.Format(GameStrings.Tr("LOG_SEC_CONVOS"), log?.Conversations.Count ?? 0));
        if (log != null)
            foreach (var id in log.Conversations)
                AddEntry(ConversationTitle(id), "", "");

        // --- Codex ---
        AddSection(string.Format(GameStrings.Tr("LOG_SEC_CODEX"),
            log?.Codex.Count ?? 0, NarrativeData.CodexEntries.Count));
        if (log != null)
            foreach (var id in log.Codex)
            {
                var e = NarrativeData.FindCodex(id);
                if (e != null) AddEntry(e.Name, e.Body + "  Weakness: " + e.Weakness, "");
            }

        // --- Lore notes ---
        AddSection(string.Format(GameStrings.Tr("LOG_SEC_NOTES"),
            log?.Notes.Count ?? 0, NarrativeData.LoreCount));
        if (log != null)
            foreach (var id in log.Notes)
            {
                var n = NarrativeData.FindNote(id);
                if (n != null) AddEntry(n.Title, string.Join(" ", n.Lines), n.Zone);
            }
    }

    private void AddSection(string text)
    {
        _content.AddChild(new Control { CustomMinimumSize = new Vector2(0, UiTheme.GapPair) });
        _content.AddChild(UiTheme.Role(UiTheme.TypeRole.Eyebrow, text, wrap: true));
        _content.AddChild(UiTheme.Divider(0.18f));
    }

    private void AddEntry(string title, string body, string zone)
    {
        var entry = new VBoxContainer();
        entry.AddThemeConstantOverride("separation", UiTheme.GapPair);
        _content.AddChild(entry);
        if (!string.IsNullOrEmpty(zone)) entry.AddChild(UiTheme.Role(UiTheme.TypeRole.Hint, zone));
        entry.AddChild(UiTheme.Role(UiTheme.TypeRole.Name, title, wrap: true));
        if (!string.IsNullOrEmpty(body))
        {
            var text = UiTheme.Role(UiTheme.TypeRole.Meta, body, wrap: true);
            entry.AddChild(text);
        }
        _content.AddChild(new Control { CustomMinimumSize = new Vector2(0, UiTheme.GapPair) });
    }

    private static string ConversationTitle(string id) => id switch
    {
        DialogueData.GrannyLan => DialogueData.SpeakerGranny + " (First Meeting)",
        DialogueData.Starfish => DialogueData.SpeakerStarfish,
        DialogueData.Seaweed => DialogueData.SpeakerSeaweed,
        DialogueData.JellyfishBox => DialogueData.SpeakerBox,
        DialogueData.BossDefeated => "Brood Mother · Restored",
        NarrativeData.Opening => "Opening · Tidepool Nursery",
        NarrativeData.GrannyMid => DialogueData.SpeakerGranny + " (On the Way)",
        NarrativeData.GrannyAfter => DialogueData.SpeakerGranny + " (After Restoration)",
        NarrativeData.StarfishDeep => DialogueData.SpeakerStarfish + " (More)",
        NarrativeData.SeaweedDeep => DialogueData.SpeakerSeaweed + " (More)",
        NarrativeData.Hermit => DialogueData.SpeakerHermit,
        NarrativeData.Lantern => DialogueData.SpeakerLantern,
        NarrativeData.Shoal => DialogueData.SpeakerShoal,
        NarrativeData.BossPre => "Brood Mother · Cry for Help",
        NarrativeData.BossMid => "Brood Mother · Release",
        NarrativeData.Ending => "Finale · Tidal Key",
        NarrativeData.Chapter2Opening => "Opening · Frostbound Trench",
        NarrativeData.Chapter2Guardian => "Frostshell Guardian · First Contact",
        NarrativeData.Chapter2Ending => "Frostbound Trench · Restored",
        NarrativeData.Chapter3Opening => "Opening · The Silent Lighthouse",
        NarrativeData.Chapter3Guardian => "Overheated Core · First Contact",
        NarrativeData.Chapter3Ending => "The Silent Lighthouse · Restored",
        _ => id,
    };

}

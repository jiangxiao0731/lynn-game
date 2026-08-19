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
        _panel.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 24, pad: 36));
        AddChild(_panel);

        var outer = new VBoxContainer();
        outer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        outer.AddThemeConstantOverride("separation", 14);
        _panel.AddChild(outer);

        outer.AddChild(MakeLabel(GameStrings.Tr("LOG_TITLE"), UiTheme.FontH1, Palette.CoastalCyan));
        outer.AddChild(MakeLabel(GameStrings.Tr("LOG_HINT"), UiTheme.FontSmall, UiTheme.InkDim));
        outer.AddChild(UiTheme.Divider());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        outer.AddChild(scroll);

        _content = new VBoxContainer();
        _content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _content.AddThemeConstantOverride("separation", 10);
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
                if (e != null) AddEntry(e.Name, e.Body + "  弱点：" + e.Weakness, "");
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
        _content.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        _content.AddChild(MakeLabel(text, UiTheme.FontH2, Palette.CoastalCyan));
        _content.AddChild(UiTheme.Divider(0.18f));
    }

    private void AddEntry(string title, string body, string zone)
    {
        string head = string.IsNullOrEmpty(zone) ? $"· {title}" : $"· {title}  〔{zone}〕";
        _content.AddChild(MakeLabel(head, UiTheme.FontBody, UiTheme.Ink));
        if (!string.IsNullOrEmpty(body))
            _content.AddChild(MakeLabel("   " + body, UiTheme.FontSmall, UiTheme.InkDim));
    }

    private static string ConversationTitle(string id) => id switch
    {
        DialogueData.GrannyLan => DialogueData.SpeakerGranny + "（初遇）",
        DialogueData.Starfish => DialogueData.SpeakerStarfish,
        DialogueData.Seaweed => DialogueData.SpeakerSeaweed,
        DialogueData.JellyfishBox => DialogueData.SpeakerBox,
        DialogueData.BossDefeated => "巢母·净化",
        NarrativeData.Opening => "开场·潮湾苗圃",
        NarrativeData.GrannyMid => DialogueData.SpeakerGranny + "（途中）",
        NarrativeData.GrannyAfter => DialogueData.SpeakerGranny + "（净化后）",
        NarrativeData.StarfishDeep => DialogueData.SpeakerStarfish + "（深谈）",
        NarrativeData.SeaweedDeep => DialogueData.SpeakerSeaweed + "（深谈）",
        NarrativeData.Hermit => DialogueData.SpeakerHermit,
        NarrativeData.Lantern => DialogueData.SpeakerLantern,
        NarrativeData.Shoal => DialogueData.SpeakerShoal,
        NarrativeData.BossPre => "巢母·求救",
        NarrativeData.BossMid => "巢母·松开",
        NarrativeData.Ending => "结章·潮汐钥匙",
        _ => id,
    };

    private static Label MakeLabel(string text, int size, Color color) =>
        UiTheme.MakeLabel(text, size, color, wrap: true);
}

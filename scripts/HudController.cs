using System.Collections.Generic;
using Godot;

namespace ShallowSeaDream;

/// Portfolio HUD with progressive disclosure. Only the current objective is shown;
/// health and unlocked abilities sit at the lower edge, while boss information and
/// status messages appear only when they are relevant.
public partial class HudController : CanvasLayer
{
    private Label _objectiveLabel = null!;
    /// "STEP 2 OF 6" — which step, not just what to do.
    private Label _objectiveStep = null!;
    private readonly List<Panel> _stepPips = new();
    private const int StepCount = 6;   // FindNpc .. ExitLevel
    private Label _objectiveMeta = null!;
    /// What the current step is about (who to find, what to collect), next to the text.
    private PanelContainer _objectiveThumbFrame = null!;
    private TextureRect _objectiveThumb = null!;
    private TextureRect _playerThumb = null!;
    private float _pictureTimer;
    private ProgressBar _objectiveProgress = null!;
    private Label _zoneLabel = null!;
    private PanelContainer _locationPanel = null!;
    private Tween? _zoneTween;
    /// How long the zone name stays fully visible before fading away.
    private const float ZoneHoldSeconds = 2.6f;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private readonly Label?[] _skillCounts = new Label?[3];
    private PanelContainer _skillDock = null!;
    private PanelContainer _bossPanel = null!;
    private Label _bossNameLabel = null!;
    private Label _bossHpLabel = null!;
    private ProgressBar _bossBar = null!;
    private PanelContainer _statusPanel = null!;
    private Label _statusLabel = null!;
    /// Leading uppercase segment of a status line ("CHAPTER 2 · FROSTBOUND TRENCH"),
    /// shown as a kicker above the message instead of run into it.
    private Label _statusKicker = null!;
    private PanelContainer _pausePanel = null!;
    private int _statusRevision;
    private int _shardCollected;
    private int _shardRequired = GameConstants.ShardThresholdForBoss;

    private SettlementPanel? _settlement;
    private FailurePanel? _failure;
    private LogPanel _log = null!;
    private readonly List<Control> _hudChrome = new();

    public override void _Ready()
    {
        Layer = 10;
        _settlement = GetNodeOrNull<SettlementPanel>("SettlementPanel");
        _failure = GetNodeOrNull<FailurePanel>("FailurePanel");
        BuildHud();

        ProcessMode = ProcessModeEnum.Always;
        _log = new LogPanel { Name = "LogPanel" };
        AddChild(_log);

        var bus = Events.Instance;
        if (bus != null)
        {
            bus.PlayerHealthChanged += OnPlayerHealthChanged;
            bus.ElementChargesChanged += OnChargesChanged;
            bus.ShardProgressChanged += OnShardProgressChanged;
            bus.ObjectiveAdvanced += OnObjectiveAdvanced;
            bus.BossHealthChanged += OnBossHealthChanged;
            bus.StatusHint += OnStatusHint;
            bus.ZoneEntered += OnZoneEntered;
            bus.PlayerDefeated += OnPlayerDefeated;
            bus.ObjectiveCompleted += OnObjectiveCompleted;
            bus.GamePaused += OnGamePaused;
            bus.DialogueStarted += OnDialogueStarted;
            bus.DialogueFinished += OnDialogueFinished;
        }

        OnObjectiveAdvanced((int)ObjectiveStage.FindNpc);
        OnPlayerHealthChanged(GameConstants.MaxHealth, GameConstants.MaxHealth);
        OnStatusHint(GameStrings.Tr("STATUS_DEFAULT_HINT"));
    }

    /// A HUD surface that grows to fit its text. These used to be fixed-size Panels,
    /// so any change to the type scale either clipped text or left dead space; the
    /// size passed in is now only a minimum.
    private static PanelContainer Surface(string name, Vector2 size, float alpha = 0.78f)
    {
        var panel = new PanelContainer { Name = name, CustomMinimumSize = size };
        panel.AddThemeStyleboxOverride("panel", UiTheme.GlassPanel(10, alpha, 0));
        return panel;
    }

    /// Anchor a surface to a screen corner and make it grow away from that corner,
    /// so a panel pinned to the bottom grows upward instead of off the screen.
    private static void Pin(Control panel, Control.LayoutPreset preset)
    {
        panel.SetAnchorsPreset(preset);
        panel.GrowHorizontal = preset switch
        {
            Control.LayoutPreset.TopRight or Control.LayoutPreset.BottomRight => Control.GrowDirection.Begin,
            Control.LayoutPreset.CenterBottom or Control.LayoutPreset.CenterTop or Control.LayoutPreset.Center
                => Control.GrowDirection.Both,
            _ => Control.GrowDirection.End,
        };
        panel.GrowVertical = preset switch
        {
            Control.LayoutPreset.BottomLeft or Control.LayoutPreset.BottomRight or Control.LayoutPreset.CenterBottom
                => Control.GrowDirection.Begin,
            Control.LayoutPreset.Center => Control.GrowDirection.Both,
            _ => Control.GrowDirection.End,
        };
    }

    private T Track<T>(T control) where T : Control
    {
        _hudChrome.Add(control);
        return control;
    }

    private static MarginContainer Inset(Control parent, int h, int v)
    {
        var inset = new MarginContainer();
        inset.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        inset.AddThemeConstantOverride("margin_left", h);
        inset.AddThemeConstantOverride("margin_right", h);
        inset.AddThemeConstantOverride("margin_top", v);
        inset.AddThemeConstantOverride("margin_bottom", v);
        parent.AddChild(inset);
        return inset;
    }

    private static StyleBox BarTrack() =>
        UiTheme.BrushBar(new Color(UiTheme.GlassBgDeep, .88f), track: true);

    private static StyleBox BarFill(Color color) => UiTheme.BrushBar(color);

    private static void StyleBar(ProgressBar bar, Color fill)
    {
        bar.ShowPercentage = false;
        bar.AddThemeStyleboxOverride("background", BarTrack());
        bar.AddThemeStyleboxOverride("fill", BarFill(fill));
    }

    private void BuildHud()
    {
        // Current objective: one readable instruction, never the full quest chain.
        var objective = Track(Surface("ObjectivePanel", new Vector2(620, 0)));
        Pin(objective, Control.LayoutPreset.TopLeft);
        objective.Position = new Vector2(UiTheme.SafeArea, UiTheme.SafeArea);
        AddChild(objective);
        var objectiveInset = Inset(objective, UiTheme.PadSurfaceX, UiTheme.PadSurfaceY);
        var objectiveRow = new HBoxContainer();
        objectiveRow.AddThemeConstantOverride("separation", UiTheme.GapBlock);
        objectiveInset.AddChild(objectiveRow);
        _objectiveThumbFrame = UiTheme.Thumbnail(72, out _objectiveThumb);
        _objectiveThumbFrame.Visible = false;
        objectiveRow.AddChild(_objectiveThumbFrame);
        var objectiveBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        objectiveBox.AddThemeConstantOverride("separation", UiTheme.GapPair);
        objectiveRow.AddChild(objectiveBox);
        _objectiveStep = UiTheme.Role(UiTheme.TypeRole.Eyebrow, "Step 1 of 6");
        objectiveBox.AddChild(_objectiveStep);
        _objectiveLabel = UiTheme.Role(UiTheme.TypeRole.Primary, "", wrap: true);
        objectiveBox.AddChild(_objectiveLabel);
        objectiveBox.AddChild(BuildStepPips());
        _objectiveMeta = UiTheme.Role(UiTheme.TypeRole.Meta, "");
        _objectiveMeta.Visible = false;
        objectiveBox.AddChild(_objectiveMeta);
        _objectiveProgress = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 5),
            MinValue = 0,
            MaxValue = _shardRequired,
            Visible = false,
        };
        StyleBar(_objectiveProgress, UiTheme.Accent);
        objectiveBox.AddChild(_objectiveProgress);

        // Location is orientation, not a faux minimap. The log shortcut is secondary.
        var location = Track(Surface("LocationPanel", new Vector2(310, 0), 0.66f));
        Pin(location, Control.LayoutPreset.TopRight);
        location.Position = new Vector2(-382, UiTheme.SafeArea);
        AddChild(location);
        var locationInset = Inset(location, UiTheme.PadSurfaceX, UiTheme.PadSurfaceY);
        var locationBox = new VBoxContainer();
        locationBox.AddThemeConstantOverride("separation", UiTheme.GapPair);
        locationInset.AddChild(locationBox);
        _zoneLabel = UiTheme.Role(UiTheme.TypeRole.Name, ChapterRuntime.Zones[0]);
        _zoneLabel.HorizontalAlignment = HorizontalAlignment.Right;
        locationBox.AddChild(_zoneLabel);
        // The panel announces a zone change and then gets out of the way; it used to
        // sit there permanently with a key hint that only mattered once.
        _locationPanel = location;
        _locationPanel.Modulate = Colors.Transparent;

        // Compact health strip. The old 220px ring obscured too much of the world.
        var health = Track(Surface("HealthPanel", new Vector2(310, 0), 0.78f));
        Pin(health, Control.LayoutPreset.BottomLeft);
        health.Position = new Vector2(UiTheme.SafeArea, -144);
        AddChild(health);
        var healthInset = Inset(health, UiTheme.PadSurfaceX, UiTheme.PadSurfaceY);
        var healthBox = new VBoxContainer();
        healthBox.AddThemeConstantOverride("separation", UiTheme.GapPair);
        healthInset.AddChild(healthBox);
        var healthTop = new HBoxContainer();
        healthTop.AddThemeConstantOverride("separation", UiTheme.GapPair);
        healthBox.AddChild(healthTop);
        healthTop.AddChild(UiTheme.Thumbnail(32, out _playerThumb));
        var hpKicker = UiTheme.Role(UiTheme.TypeRole.Eyebrow, "Shimmer");
        hpKicker.VerticalAlignment = VerticalAlignment.Center;
        healthTop.AddChild(hpKicker);
        _hpLabel = UiTheme.Role(UiTheme.TypeRole.Numeral, "");
        _hpLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _hpLabel.HorizontalAlignment = HorizontalAlignment.Right;
        healthTop.AddChild(_hpLabel);
        _hpBar = new ProgressBar { CustomMinimumSize = new Vector2(0, 8), MinValue = 0 };
        StyleBar(_hpBar, UiTheme.Accent);
        healthBox.AddChild(_hpBar);

        // Ability charges appear only after the mechanic is unlocked.
        _skillDock = Track(Surface("SkillDock", new Vector2(430, 0), 0.72f));
        Pin(_skillDock, Control.LayoutPreset.CenterBottom);
        _skillDock.Position = new Vector2(-215, -152);
        _skillDock.Visible = false;
        AddChild(_skillDock);
        var skillInset = Inset(_skillDock, UiTheme.PadSurfaceX, UiTheme.PadSurfaceY);
        var skillRow = new HBoxContainer();
        skillRow.AddThemeConstantOverride("separation", UiTheme.GapPair);
        skillInset.AddChild(skillRow);
        var skills = new[]
        {
            ("1", "WATER", Palette.ElementWater),
            ("2", "ICE", Palette.ElementIce),
            ("3", "ELECTRIC", Palette.ElementElectric),
        };
        // Ice unlocks at the end of chapter one and Electric at the end of chapter two,
        // so earlier chapters would otherwise show slots that can only ever read 0.
        int unlocked = Mathf.Clamp(ChapterRuntime.CurrentChapter, 1, 3);
        for (int i = 0; i < skills.Length; i++)
        {
            if (i >= unlocked) continue;
            var slot = new HBoxContainer { CustomMinimumSize = new Vector2(126, 0) };
            slot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            slot.AddThemeConstantOverride("separation", UiTheme.GapPair);
            var key = UiTheme.Role(UiTheme.TypeRole.Hint, skills[i].Item1);
            key.VerticalAlignment = VerticalAlignment.Center;
            slot.AddChild(key);
            var formIcon = UiTheme.Thumbnail(32, out var formImage);
            var frames = PlaceholderArt.FormFrames((ElementForm)(i + 1));
            var anim = frames.GetAnimationNames();
            if (anim.Length > 0 && frames.GetFrameCount(anim[0]) > 0) formImage.Texture = frames.GetFrameTexture(anim[0], 0);
            slot.AddChild(formIcon);
            var name = UiTheme.Role(UiTheme.TypeRole.Eyebrow, skills[i].Item2);
            name.AddThemeColorOverride("font_color", skills[i].Item3);
            name.VerticalAlignment = VerticalAlignment.Center;
            slot.AddChild(name);
            var count = UiTheme.Role(UiTheme.TypeRole.Numeral, "0");
            count.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            count.HorizontalAlignment = HorizontalAlignment.Right;
            _skillCounts[i] = count;
            slot.AddChild(count);
            skillRow.AddChild(slot);
        }

        // Boss name and health get the conventional top-centre focal position.
        _bossPanel = Track(Surface("BossPanel", new Vector2(760, 0), 0.86f));
        Pin(_bossPanel, Control.LayoutPreset.CenterTop);
        _bossPanel.Position = new Vector2(-380, UiTheme.SafeArea);
        _bossPanel.Visible = false;
        AddChild(_bossPanel);
        var bossInset = Inset(_bossPanel, UiTheme.PadSurfaceX, UiTheme.PadSurfaceY);
        var bossBox = new VBoxContainer();
        bossBox.AddThemeConstantOverride("separation", UiTheme.GapPair);
        bossInset.AddChild(bossBox);
        var bossTop = new HBoxContainer();
        bossTop.AddThemeConstantOverride("separation", UiTheme.GapPair);
        bossBox.AddChild(bossTop);
        var bossThumb = UiTheme.Thumbnail(40, out var bossImage);
        bossImage.Texture = AssetLoader.Texture(AssetLoader.ChapterBossPortrait(ChapterRuntime.CurrentChapter));
        bossThumb.Visible = bossImage.Texture != null;
        bossTop.AddChild(bossThumb);
        _bossNameLabel = UiTheme.Role(UiTheme.TypeRole.Name, ChapterRuntime.BossName);
        bossTop.AddChild(_bossNameLabel);
        _bossHpLabel = UiTheme.Role(UiTheme.TypeRole.Numeral, "");
        _bossHpLabel.AddThemeColorOverride("font_color", UiTheme.InkDim);
        _bossHpLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _bossHpLabel.HorizontalAlignment = HorizontalAlignment.Right;
        bossTop.AddChild(_bossHpLabel);
        _bossBar = new ProgressBar { CustomMinimumSize = new Vector2(0, 8), MinValue = 0 };
        StyleBar(_bossBar, Palette.PollutedTeal);
        bossBox.AddChild(_bossBar);

        // A transient toast replaces the permanent full-width instruction strip.
        _statusPanel = Track(Surface("StatusToast", new Vector2(880, 0), 0.90f));
        Pin(_statusPanel, Control.LayoutPreset.CenterBottom);
        _statusPanel.Position = new Vector2(-440, -264);
        AddChild(_statusPanel);
        var statusInset = Inset(_statusPanel, UiTheme.PadSurfaceX, UiTheme.PadSurfaceY);
        var statusBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        statusBox.AddThemeConstantOverride("separation", UiTheme.GapPair);
        statusInset.AddChild(statusBox);
        _statusKicker = UiTheme.Role(UiTheme.TypeRole.Eyebrow, "");
        _statusKicker.HorizontalAlignment = HorizontalAlignment.Center;
        _statusKicker.Visible = false;
        statusBox.AddChild(_statusKicker);
        _statusLabel = UiTheme.Role(UiTheme.TypeRole.Body, "", wrap: true);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        statusBox.AddChild(_statusLabel);

        _pausePanel = Surface("PauseFeedbackPanel", new Vector2(360, 0), 0.94f);
        Pin(_pausePanel, Control.LayoutPreset.Center);
        _pausePanel.Visible = false;
        _pausePanel.ProcessMode = ProcessModeEnum.Always;
        AddChild(_pausePanel);
        var pauseBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        pauseBox.AddThemeConstantOverride("separation", UiTheme.GapPair);
        Inset(_pausePanel, UiTheme.PadSurfaceX, UiTheme.PadSurfaceY).AddChild(pauseBox);
        var pauseLabel = UiTheme.Role(UiTheme.TypeRole.Heading, "Paused");
        pauseLabel.HorizontalAlignment = HorizontalAlignment.Center;
        pauseBox.AddChild(pauseLabel);
        var pauseHint = UiTheme.Role(UiTheme.TypeRole.Hint, "Esc to resume");
        pauseHint.HorizontalAlignment = HorizontalAlignment.Center;
        pauseBox.AddChild(pauseHint);
    }

    /// Thumbnails follow the world rather than a per-chapter table: whatever the guide
    /// arrow points at, and whatever form Shimmer is in. Polled a few times a second.
    public override void _Process(double delta)
    {
        _pictureTimer -= (float)delta;
        if (_pictureTimer > 0f) return;
        _pictureTimer = 0.25f;
        var guide = GetTree().CurrentScene?.FindChild("ObjectiveGuide", true, false) as ObjectiveGuide;
        var target = guide?.TargetPicture();
        _objectiveThumb.Texture = target;
        _objectiveThumbFrame.Visible = target != null;
        _playerThumb.Texture = UiTheme.PictureOf(GetTree().GetFirstNodeInGroup("player"));
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        _hpLabel.Text = $"{current} / {max}";
        _hpBar.MaxValue = max;
        _hpBar.Value = current;
    }

    private void OnChargesChanged(int water, int ice, int electric)
    {
        // Slots for forms this chapter has not unlocked are never built, so the array
        // is sparse; skip the holes instead of dereferencing them.
        var counts = new[] { water, ice, electric };
        for (int i = 0; i < _skillCounts.Length; i++)
        {
            var label = _skillCounts[i];
            if (label != null) label.Text = counts[i].ToString();
        }
    }

    private void OnShardProgressChanged(int collected, int required)
    {
        _shardCollected = collected;
        _shardRequired = required;
        _objectiveProgress.MaxValue = required;
        _objectiveProgress.Value = collected;
        _objectiveMeta.Text = $"{ChapterRuntime.FragmentLabel}  {collected} / {required}";
    }

    private void OnObjectiveAdvanced(int stage)
    {
        var current = (ObjectiveStage)stage;
        _objectiveLabel.Text = GameStrings.ObjectiveLabel(current);
        int step = Mathf.Clamp(stage, 0, StepCount);
        _objectiveStep.Text = current == ObjectiveStage.Complete
            ? "ALL STEPS DONE"
            : $"STEP {step + 1} OF {StepCount}";
        UpdateStepPips(step);
        bool collecting = current == ObjectiveStage.CollectShards;
        _objectiveMeta.Visible = collecting;
        _objectiveProgress.Visible = collecting;
        if (collecting)
            OnShardProgressChanged(_shardCollected, _shardRequired);
        _skillDock.Visible = current >= ObjectiveStage.CollectShards;
        _bossPanel.Visible = current == ObjectiveStage.DefeatBoss;

        _objectiveLabel.Modulate = new Color(1.08f, 1.08f, 1.08f, 1f);
        var tween = CreateTween();
        tween.TweenProperty(_objectiveLabel, "modulate", Colors.White, 0.35f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    /// One pip per step. Done steps fill with the accent, the current one is amber —
    /// the same amber as the world arrow, so the panel and the arrow read as one
    /// instruction — and later ones are a faint ring.
    private HBoxContainer BuildStepPips()
    {
        var row = new HBoxContainer { Name = "StepPips" };
        row.AddThemeConstantOverride("separation", UiTheme.GapPair);
        for (int i = 0; i < StepCount; i++)
        {
            var pip = new Panel { CustomMinimumSize = new Vector2(16, 16), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
            _stepPips.Add(pip);
            row.AddChild(pip);
        }
        return row;
    }


    private void UpdateStepPips(int current)
    {
        for (int i = 0; i < _stepPips.Count; i++)
        {
            bool done = i < current, now = i == current;
            var style = new StyleBoxFlat
            {
                BgColor = done ? UiTheme.Accent : now ? UiTheme.Guide : Colors.Transparent,
                BorderColor = done ? UiTheme.Accent : now ? UiTheme.Guide : new Color(UiTheme.InkDim, 0.6f),
                BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
                CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
                CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            };
            _stepPips[i].AddThemeStyleboxOverride("panel", style);
            _stepPips[i].CustomMinimumSize = now ? new Vector2(24, 24) : new Vector2(16, 16);
        }
    }

    private void OnBossHealthChanged(int current, int max)
    {
        _bossPanel.Visible = current > 0;
        _bossHpLabel.Text = $"{current} / {max}";
        _bossBar.MaxValue = max;
        _bossBar.Value = current;
    }

    private void OnZoneEntered(string zoneName)
    {
        _zoneLabel.Text = zoneName;
        _zoneLabel.Modulate = UiTheme.Accent;

        // Re-entering a zone restarts the announcement rather than stacking tweens.
        _zoneTween?.Kill();
        _zoneTween = CreateTween();
        _zoneTween.TweenProperty(_locationPanel, "modulate", Colors.White, 0.35f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _zoneTween.Parallel().TweenProperty(_zoneLabel, "modulate", UiTheme.Ink, 0.6f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _zoneTween.TweenInterval(ZoneHoldSeconds);
        _zoneTween.TweenProperty(_locationPanel, "modulate", Colors.Transparent, 0.9f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    }

    private async void OnStatusHint(string text)
    {
        int revision = ++_statusRevision;
        // Senders separate a kicker from the message with a run of spaces
        // ("CHAPTER 2 · FROSTBOUND TRENCH   Find Lanternfish and press E").
        var parts = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"\s{3,}", 
            System.Text.RegularExpressions.RegexOptions.None, System.TimeSpan.FromMilliseconds(50));
        bool hasKicker = parts.Length >= 2 && parts[0] == parts[0].ToUpperInvariant();
        _statusKicker.Visible = hasKicker;
        _statusKicker.Text = hasKicker ? parts[0].ToUpperInvariant() : "";
        _statusLabel.Text = hasKicker ? string.Join(" ", parts, 1, parts.Length - 1) : text;
        _statusPanel.Visible = true;
        _statusPanel.Modulate = Colors.White;
        await ToSignal(GetTree().CreateTimer(4.2), SceneTreeTimer.SignalName.Timeout);
        if (revision != _statusRevision || !IsInstanceValid(_statusPanel)) return;
        var tween = CreateTween();
        tween.TweenProperty(_statusPanel, "modulate:a", 0f, 0.28f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        await ToSignal(tween, Tween.SignalName.Finished);
        if (revision == _statusRevision) _statusPanel.Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_log"))
        {
            _log?.Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnPlayerDefeated() { }
    private void OnObjectiveCompleted()
    {
        if (ChapterRuntime.CurrentChapter == 1)
            _settlement?.ShowResult(GameStrings.Tr("SETTLEMENT_SUMMARY"));
    }
    private void OnGamePaused(bool paused) => _pausePanel.Visible = paused;
    private void OnDialogueStarted(string timelineId) => SetDialogueFocus(true);
    private void OnDialogueFinished(string timelineId) => SetDialogueFocus(false);

    private void SetDialogueFocus(bool active)
    {
        foreach (var control in _hudChrome)
        {
            var tween = control.CreateTween();
            tween.TweenProperty(control, "modulate:a", active ? 0.08f : 1.0f, 0.22f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            control.MouseFilter = active ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Stop;
        }
    }
}

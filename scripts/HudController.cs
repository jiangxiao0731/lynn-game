using System.Collections.Generic;
using Godot;

namespace ShallowSeaDream;

/// Portfolio HUD with progressive disclosure. Only the current objective is shown;
/// health and unlocked abilities sit at the lower edge, while boss information and
/// status messages appear only when they are relevant.
public partial class HudController : CanvasLayer
{
    private Label _objectiveLabel = null!;
    private Label _objectiveMeta = null!;
    private ProgressBar _objectiveProgress = null!;
    private Label _zoneLabel = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpBar = null!;
    private readonly Label[] _skillCounts = new Label[3];
    private Panel _skillDock = null!;
    private Panel _bossPanel = null!;
    private Label _bossNameLabel = null!;
    private Label _bossHpLabel = null!;
    private ProgressBar _bossBar = null!;
    private Panel _statusPanel = null!;
    private Label _statusLabel = null!;
    private Panel _pausePanel = null!;
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

    private static Panel Surface(string name, Vector2 size, float alpha = 0.78f)
    {
        var panel = UiTheme.MakeGlassPanel(name, radius: 10, bgAlpha: alpha, pad: 0);
        panel.CustomMinimumSize = size;
        panel.Size = size;
        return panel;
    }

    private T Track<T>(T control) where T : Control
    {
        _hudChrome.Add(control);
        return control;
    }

    private static Label Text(string text, int size, Color color, bool wrap = false) =>
        UiTheme.MakeLabel(text, size, color, wrap);

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
        UiTheme.BrushBar(new Color(.025f,.075f,.085f,.88f), track: true);

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
        var objective = Track(Surface("ObjectivePanel", new Vector2(560, 142)));
        objective.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        objective.Position = new Vector2(UiTheme.SafeArea, UiTheme.SafeArea);
        AddChild(objective);
        var objectiveInset = Inset(objective, UiTheme.Space6, UiTheme.Space4);
        var objectiveBox = new VBoxContainer();
        objectiveBox.AddThemeConstantOverride("separation", UiTheme.Space1);
        objectiveInset.AddChild(objectiveBox);
        var eyebrow = UiTheme.MakeStrongLabel("CURRENT OBJECTIVE", UiTheme.FontTiny, UiTheme.Accent);
        eyebrow.AddThemeConstantOverride("letter_spacing", 2);
        objectiveBox.AddChild(eyebrow);
        _objectiveLabel = UiTheme.MakeStrongLabel("", 23, UiTheme.Ink, true);
        objectiveBox.AddChild(_objectiveLabel);
        _objectiveMeta = Text("", UiTheme.FontTiny, UiTheme.InkDim);
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
        var location = Track(Surface("LocationPanel", new Vector2(310, 80), 0.66f));
        location.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        location.Position = new Vector2(-382, UiTheme.SafeArea);
        AddChild(location);
        var locationInset = Inset(location, UiTheme.Space4, UiTheme.Space3);
        var locationBox = new VBoxContainer();
        locationBox.AddThemeConstantOverride("separation", 0);
        locationInset.AddChild(locationBox);
        _zoneLabel = UiTheme.MakeStrongLabel(ChapterRuntime.Zones[0], UiTheme.FontSmall, UiTheme.Ink);
        _zoneLabel.HorizontalAlignment = HorizontalAlignment.Right;
        locationBox.AddChild(_zoneLabel);
        var logHint = Text("J  ·  MEMORY JOURNAL", UiTheme.FontTiny, UiTheme.InkFaint);
        logHint.HorizontalAlignment = HorizontalAlignment.Right;
        locationBox.AddChild(logHint);

        // Compact health strip. The old 220px ring obscured too much of the world.
        var health = Track(Surface("HealthPanel", new Vector2(310, 72), 0.78f));
        health.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        health.Position = new Vector2(UiTheme.SafeArea, -144);
        AddChild(health);
        var healthInset = Inset(health, UiTheme.Space4, UiTheme.Space3);
        var healthBox = new VBoxContainer();
        healthBox.AddThemeConstantOverride("separation", UiTheme.Space2);
        healthInset.AddChild(healthBox);
        var healthTop = new HBoxContainer();
        healthBox.AddChild(healthTop);
        healthTop.AddChild(UiTheme.MakeStrongLabel("SHIMMER", UiTheme.FontTiny, UiTheme.InkDim));
        _hpLabel = Text("", UiTheme.FontTiny, UiTheme.Ink);
        _hpLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _hpLabel.HorizontalAlignment = HorizontalAlignment.Right;
        healthTop.AddChild(_hpLabel);
        _hpBar = new ProgressBar { CustomMinimumSize = new Vector2(0, 8), MinValue = 0 };
        StyleBar(_hpBar, new Color(0.47f, 0.90f, 0.78f));
        healthBox.AddChild(_hpBar);

        // Ability charges appear only after the mechanic is unlocked.
        _skillDock = Track(Surface("SkillDock", new Vector2(430, 80), 0.72f));
        _skillDock.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _skillDock.Position = new Vector2(-215, -152);
        _skillDock.Visible = false;
        AddChild(_skillDock);
        var skillInset = Inset(_skillDock, UiTheme.Space3, UiTheme.Space3);
        var skillRow = new HBoxContainer();
        skillRow.AddThemeConstantOverride("separation", UiTheme.Space2);
        skillInset.AddChild(skillRow);
        var skills = new[]
        {
            ("1", "WATER", Palette.ElementWater),
            ("2", "ICE", Palette.ElementIce),
            ("3", "ELECTRIC", Palette.ElementElectric),
        };
        for (int i = 0; i < skills.Length; i++)
        {
            var slot = new HBoxContainer { CustomMinimumSize = new Vector2(126, 0) };
            slot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            slot.AddThemeConstantOverride("separation", UiTheme.Space2);
            var key = Text(skills[i].Item1, UiTheme.FontTiny, skills[i].Item3);
            slot.AddChild(key);
            var name = UiTheme.MakeStrongLabel(skills[i].Item2, UiTheme.FontSmall, UiTheme.Ink);
            slot.AddChild(name);
            var count = Text("0", UiTheme.FontSmall, UiTheme.InkDim);
            count.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            count.HorizontalAlignment = HorizontalAlignment.Right;
            _skillCounts[i] = count;
            slot.AddChild(count);
            skillRow.AddChild(slot);
        }

        // Boss name and health get the conventional top-centre focal position.
        _bossPanel = Track(Surface("BossPanel", new Vector2(760, 96), 0.86f));
        _bossPanel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _bossPanel.Position = new Vector2(-380, UiTheme.SafeArea);
        _bossPanel.Visible = false;
        AddChild(_bossPanel);
        var bossInset = Inset(_bossPanel, UiTheme.Space6, UiTheme.Space3);
        var bossBox = new VBoxContainer();
        bossBox.AddThemeConstantOverride("separation", UiTheme.Space2);
        bossInset.AddChild(bossBox);
        var bossTop = new HBoxContainer();
        bossBox.AddChild(bossTop);
        _bossNameLabel = UiTheme.MakeStrongLabel(ChapterRuntime.BossName, UiTheme.FontSmall, UiTheme.Ink);
        bossTop.AddChild(_bossNameLabel);
        _bossHpLabel = Text("", UiTheme.FontTiny, UiTheme.InkDim);
        _bossHpLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _bossHpLabel.HorizontalAlignment = HorizontalAlignment.Right;
        bossTop.AddChild(_bossHpLabel);
        _bossBar = new ProgressBar { CustomMinimumSize = new Vector2(0, 8), MinValue = 0 };
        StyleBar(_bossBar, Palette.PollutedTeal);
        bossBox.AddChild(_bossBar);

        // A transient toast replaces the permanent full-width instruction strip.
        _statusPanel = Track(Surface("StatusToast", new Vector2(880, 96), 0.90f));
        _statusPanel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _statusPanel.Position = new Vector2(-440, -264);
        AddChild(_statusPanel);
        var statusInset = Inset(_statusPanel, UiTheme.Space6, UiTheme.Space3);
        _statusLabel = Text("", UiTheme.FontSmall, UiTheme.Ink, true);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.VerticalAlignment = VerticalAlignment.Center;
        statusInset.AddChild(_statusLabel);

        _pausePanel = Surface("PauseFeedbackPanel", new Vector2(360, 112), 0.94f);
        _pausePanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _pausePanel.Position = new Vector2(-180, -56);
        _pausePanel.Visible = false;
        _pausePanel.ProcessMode = ProcessModeEnum.Always;
        AddChild(_pausePanel);
        var pauseLabel = UiTheme.MakeDisplayLabel("PAUSED", UiTheme.FontH2, UiTheme.Ink);
        pauseLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        pauseLabel.HorizontalAlignment = HorizontalAlignment.Center;
        pauseLabel.VerticalAlignment = VerticalAlignment.Center;
        _pausePanel.AddChild(pauseLabel);
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        _hpLabel.Text = $"{current} / {max}";
        _hpBar.MaxValue = max;
        _hpBar.Value = current;
    }

    private void OnChargesChanged(int water, int ice, int electric)
    {
        _skillCounts[0].Text = water.ToString();
        _skillCounts[1].Text = ice.ToString();
        _skillCounts[2].Text = electric.ToString();
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
        var tween = CreateTween();
        tween.TweenProperty(_zoneLabel, "modulate", UiTheme.Ink, 0.6f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    private async void OnStatusHint(string text)
    {
        int revision = ++_statusRevision;
        _statusLabel.Text = text;
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

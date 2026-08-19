using System;
using System.Collections.Generic;
using Godot;

namespace ShallowSeaDream;

/// res://scripts/DialogueRunner.cs
/// Character-bound speech bubbles. Each line follows its speaker in screen space,
/// flips above/below near viewport edges, and carries choices in the same bubble.
/// Narration and memories attach to the player instead of occupying a bottom bar.
public partial class DialogueRunner : CanvasLayer
{
    [Signal] public delegate void FinishedEventHandler(string timelineId);

    private const float CharsPerSecond = 42f;

    private Panel _panel = null!;
    private Panel _portraitFrame = null!;
    private PanelContainer _speakerTab = null!;
    private TextureRect _portrait = null!;
    private Label _speakerLabel = null!;
    private Label _textLabel = null!;
    private Label _hintLabel = null!;
    private VBoxContainer _choiceBox = null!;
    private Polygon2D _tail = null!;
    private Node2D? _speakerAnchor;

    private static readonly Vector2 BubbleSize = new(700, 214);
    private static readonly Vector2 ChoiceBubbleSize = new(780, 374);

    private DialogueTimeline? _timeline;
    private int _lineIndex;
    private Action? _onComplete;

    // Typewriter state.
    private string _fullText = "";
    private float _revealChars;
    private bool _revealing;
    private bool _choicesOpen;

    public bool IsActive => _timeline != null;

    public override void _Ready()
    {
        Layer = 20;
        BuildUi();
        _panel.Visible = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildUi()
    {
        _panel = UiTheme.MakeGlassPanel("SpeechBubble", radius: 12, bgAlpha: 0.95f, pad: 0);
        _panel.Size = BubbleSize;
        _panel.CustomMinimumSize = BubbleSize;
        _panel.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(_panel);

        _tail = new Polygon2D
        {
            Name = "BubbleTail",
            Polygon = new[] { new Vector2(-18, 0), new Vector2(18, 0), new Vector2(0, 28) },
            Color = UiTheme.GlassBgDeep,
            ZIndex = -1,
            ShowBehindParent = true,
        };
        _panel.AddChild(_tail);

        var inset = new MarginContainer();
        inset.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        inset.AddThemeConstantOverride("margin_left", UiTheme.Space6);
        inset.AddThemeConstantOverride("margin_right", UiTheme.Space6);
        inset.AddThemeConstantOverride("margin_top", UiTheme.Space4);
        inset.AddThemeConstantOverride("margin_bottom", UiTheme.Space4);
        _panel.AddChild(inset);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UiTheme.Space4);
        inset.AddChild(hbox);

        _portraitFrame = UiTheme.MakeGlassPanel("PortraitFrame", radius: 8, bgAlpha: 0.28f, pad: 6);
        _portraitFrame.CustomMinimumSize = new Vector2(96, 96);
        _portraitFrame.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        hbox.AddChild(_portraitFrame);

        _portrait = new TextureRect
        {
            Name = "Portrait",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        _portrait.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _portraitFrame.AddChild(_portrait);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", UiTheme.Space2);
        hbox.AddChild(vbox);

        // Speaker is an eyebrow, not another nested card.
        _speakerTab = new PanelContainer { Name = "SpeakerTab" };
        var tabStyle = new StyleBoxFlat { BgColor = Colors.Transparent };
        _speakerTab.AddThemeStyleboxOverride("panel", tabStyle);
        _speakerTab.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        vbox.AddChild(_speakerTab);

        _speakerLabel = UiTheme.MakeDisplayLabel("", UiTheme.FontSmall, UiTheme.Accent);
        _speakerTab.AddChild(_speakerLabel);

        _textLabel = UiTheme.MakeLabel("", UiTheme.FontBody, UiTheme.Ink, wrap: true);
        _textLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _textLabel.AddThemeConstantOverride("line_spacing", 9);
        _textLabel.CustomMinimumSize = new Vector2(0, 112);
        vbox.AddChild(_textLabel);

        _choiceBox = new VBoxContainer { Name = "Choices" };
        _choiceBox.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(_choiceBox);

        _hintLabel = UiTheme.MakeLabel("SPACE / ENTER  ·  CONTINUE", UiTheme.FontTiny, UiTheme.InkFaint);
        _hintLabel.HorizontalAlignment = HorizontalAlignment.Right;
        vbox.AddChild(_hintLabel);
    }

    /// Begin a registered timeline by id; onComplete fires once after the last line.
    public void Play(string timelineId, Action? onComplete = null)
    {
        var timeline = DialogueData.Get(timelineId);
        if (timeline == null || timeline.Lines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
        _timeline = timeline;
        _onComplete = onComplete;
        _lineIndex = -1;
        _panel.Visible = true;
        MemoryLog.Instance?.RecordConversation(timelineId);
        AudioManager.Instance?.PlaySfx("dialogue_start");
        Events.Instance?.EmitSignal(Events.SignalName.DialogueStarted, timelineId);
        ShowNextLine();
    }

    /// Show an ad-hoc vignette (memory / lore note) as a titled, speaker-less timeline.
    public void ShowVignette(string title, IReadOnlyList<string> lines, string speaker, Action? onComplete = null)
    {
        var built = new List<DialogueLine>();
        foreach (var ln in lines) built.Add(new DialogueLine(speaker, ln));
        _timeline = new DialogueTimeline("__vignette__", built);
        _onComplete = onComplete;
        _lineIndex = -1;
        _panel.Visible = true;
        AudioManager.Instance?.PlaySfx("dialogue_start");
        Events.Instance?.EmitSignal(Events.SignalName.DialogueStarted, title);
        ShowNextLine();
    }

    public override void _Process(double delta)
    {
        if (IsActive) UpdateBubblePosition();
        if (!_revealing) return;
        _revealChars += (float)delta * CharsPerSecond;
        int show = Mathf.Min(_fullText.Length, Mathf.FloorToInt(_revealChars));
        _textLabel.Text = _fullText.Substring(0, show);
        if (show >= _fullText.Length) _revealing = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsActive || _choicesOpen) return;
        if (@event.IsActionPressed("advance_dialog") || @event.IsActionPressed("e"))
        {
            if (_revealing)
            {
                // First press: reveal the whole line instantly (skip-to-full).
                _revealing = false;
                _textLabel.Text = _fullText;
            }
            else
            {
                ShowNextLine();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    private void ShowNextLine()
    {
        if (_timeline == null) return;
        _lineIndex++;
        if (_lineIndex >= _timeline.Lines.Count)
        {
            Finish();
            return;
        }
        var line = _timeline.Lines[_lineIndex];
        _speakerLabel.Text = line.Speaker;
        _speakerTab.Visible = !string.IsNullOrEmpty(line.Speaker);
        _speakerAnchor = ResolveSpeakerAnchor(line.Speaker);
        // Lines rarely carry an explicit PortraitId, so fall back to mapping the
        // speaker name → portrait asset id (npc_{id}.png) for a meaningful frame.
        SetPortrait(line.PortraitId ?? PortraitForSpeaker(line.Speaker));
        StartTypewriter(line.Text);
        AnimateBubbleEntry();
        Events.Instance?.EmitSignal(Events.SignalName.DialogueLineStarted, line.Speaker, line.Text);
        AudioManager.Instance?.PlayDialogue(null, line.Speaker, line.Text);

        if (line.Choices is { Count: > 0 })
            OpenChoices(line.Choices);
    }

    private void StartTypewriter(string text)
    {
        _fullText = text;
        _revealChars = 0f;
        _revealing = true;
        _textLabel.Text = "";
    }

    private Node2D? ResolveSpeakerAnchor(string speaker)
    {
        if (speaker == DialogueData.SpeakerShimmer || speaker == "Fragment" || string.IsNullOrEmpty(speaker))
            return GetTree().GetFirstNodeInGroup("player") as Node2D;

        string? nodeName = speaker switch
        {
            DialogueData.SpeakerGranny => "GrannyLan",
            DialogueData.SpeakerStarfish => "StarfishNPC",
            DialogueData.SpeakerSeaweed => "SeaweedNPC",
            DialogueData.SpeakerBox => "GasolineBarrel",
            DialogueData.SpeakerHermit => "HermitNPC",
            DialogueData.SpeakerLantern => "LanternNPC",
            DialogueData.SpeakerShoal => "ShoalNPC",
            DialogueData.SpeakerBoss => "BroodMother",
            DialogueData.SpeakerFrostshell => "BroodMother",
            DialogueData.SpeakerCore => "BroodMother",
            _ => null,
        };
        if (nodeName == null) return GetTree().GetFirstNodeInGroup("player") as Node2D;
        return GetTree().CurrentScene?.FindChild(nodeName, recursive: true, owned: false) as Node2D
               ?? GetTree().GetFirstNodeInGroup("player") as Node2D;
    }

    private void UpdateBubblePosition()
    {
        if (_speakerAnchor == null || !IsInstanceValid(_speakerAnchor)) return;
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 speakerScreen = GetViewport().GetCanvasTransform() * _speakerAnchor.GlobalPosition;
        Vector2 bubbleSize = _panel.Size;
        const float safe = 32f;
        const float actorGap = 68f;

        bool placeAbove = speakerScreen.Y >= bubbleSize.Y + actorGap + safe;
        float desiredY = placeAbove
            ? speakerScreen.Y - bubbleSize.Y - actorGap
            : speakerScreen.Y + actorGap;
        float maxX = Mathf.Max(safe, viewportSize.X - bubbleSize.X - safe);
        float maxY = Mathf.Max(safe, viewportSize.Y - bubbleSize.Y - safe);
        var position = new Vector2(
            Mathf.Clamp(speakerScreen.X - bubbleSize.X * 0.5f, safe, maxX),
            Mathf.Clamp(desiredY, safe, maxY));
        _panel.Position = position;

        float tailX = Mathf.Clamp(speakerScreen.X - position.X, 30f, bubbleSize.X - 30f);
        if (placeAbove)
        {
            _tail.Position = new Vector2(tailX, bubbleSize.Y - 1f);
            _tail.Polygon = new[] { new Vector2(-18, 0), new Vector2(18, 0), new Vector2(0, 28) };
        }
        else
        {
            _tail.Position = new Vector2(tailX, 1f);
            _tail.Polygon = new[] { new Vector2(-18, 0), new Vector2(18, 0), new Vector2(0, -28) };
        }
    }

    private void AnimateBubbleEntry()
    {
        _panel.PivotOffset = _panel.Size * 0.5f;
        _panel.Scale = new Vector2(0.98f, 0.98f);
        _panel.Modulate = new Color(1f, 1f, 1f, 0.55f);
        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_panel, "scale", Vector2.One, 0.16f)
            .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_panel, "modulate:a", 1f, 0.13f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    /// Map a speaker display name to its portrait asset id (npc_{id}.png).
    private static string? PortraitForSpeaker(string speaker) => speaker switch
    {
        DialogueData.SpeakerGranny => DialogueData.GrannyLan, // npc1giving
        DialogueData.SpeakerStarfish => DialogueData.Starfish,
        DialogueData.SpeakerSeaweed => DialogueData.Seaweed,
        DialogueData.SpeakerBox => "barrel",
        DialogueData.SpeakerHermit => "hermit",
        DialogueData.SpeakerLantern => "lantern",
        DialogueData.SpeakerShoal => "shoal",
        DialogueData.SpeakerBoss => "boss",
        DialogueData.SpeakerFrostshell => "boss",
        DialogueData.SpeakerCore => "boss",
        _ => null, // 微光 / 残片 narration: no portrait
    };

    private void SetPortrait(string? portraitId)
    {
        if (string.IsNullOrEmpty(portraitId))
        {
            _portrait.Texture = null;
            _portraitFrame.Visible = false;
            return;
        }
        _portraitFrame.Visible = true;
        var tex = AssetLoader.Texture(AssetLoader.NpcPortrait(portraitId));
        _portrait.Texture = tex ?? PlaceholderArt.RoundBlob(160, new Color(0.45f, 0.7f, 0.78f));
    }

    private void OpenChoices(IReadOnlyList<DialogueChoice> choices)
    {
        _choicesOpen = true;
        _panel.Size = ChoiceBubbleSize;
        _panel.CustomMinimumSize = ChoiceBubbleSize;
        _hintLabel.Visible = false;
        // Reveal instantly so the player can read the prompt before choosing.
        _revealing = false;
        _textLabel.Text = _fullText;

        foreach (var choice in choices)
        {
            var btn = new Button { Text = choice.Text };
            btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            UiTheme.StyleButton(btn, UiTheme.FontSmall, primary: false);
            btn.CustomMinimumSize = new Vector2(0, 48);
            var captured = choice;
            btn.Pressed += () => OnChoicePicked(captured);
            _choiceBox.AddChild(btn);
        }
    }

    private void OnChoicePicked(DialogueChoice choice)
    {
        if (!string.IsNullOrEmpty(choice.SetFlag))
            MemoryLog.Instance?.SetFlag(choice.SetFlag);
        ClearChoices();
        AudioManager.Instance?.PlaySfx("dialogue_start");

        if (!string.IsNullOrEmpty(choice.GotoLabel) && _timeline != null)
        {
            int target = FindLabel(choice.GotoLabel!);
            if (target >= 0)
            {
                _lineIndex = target - 1; // ShowNextLine will ++ it
                ShowNextLine();
                return;
            }
        }
        ShowNextLine();
    }

    private int FindLabel(string label)
    {
        if (_timeline == null) return -1;
        for (int i = 0; i < _timeline.Lines.Count; i++)
            if (_timeline.Lines[i].Label == label) return i;
        return -1;
    }

    private void ClearChoices()
    {
        foreach (var c in _choiceBox.GetChildren()) c.QueueFree();
        _choicesOpen = false;
        _panel.Size = BubbleSize;
        _panel.CustomMinimumSize = BubbleSize;
        _hintLabel.Visible = true;
    }

    private void Finish()
    {
        string id = _timeline?.Id ?? "";
        _timeline = null;
        _revealing = false;
        ClearChoices();
        _panel.Visible = false;
        Events.Instance?.EmitSignal(Events.SignalName.DialogueFinished, id);
        EmitSignal(SignalName.Finished, id);
        var cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }
}

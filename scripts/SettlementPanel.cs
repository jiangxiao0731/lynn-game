using Godot;

namespace ShallowSeaDream;

/// res://scripts/SettlementPanel.cs
/// Win screen shown on OBJECTIVE_COMPLETE. Title + summary + continue button.
public partial class SettlementPanel : Control
{
    [Export] public string TitleScenePath = "res://scenes/title.tscn";
    [Export] public string NextScenePath = "";

    private Label? _summary;

    public override void _Ready()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;

        // ── Deep-sea glass panel background ──────────────────────────────────
        var box = GetNodeOrNull<PanelContainer>("Box");
        if (box != null)
        {
            box.CustomMinimumSize = new Vector2(840, 440);
            box.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 24, pad: UiTheme.PadScreen));
        }

        var layout = GetNodeOrNull<VBoxContainer>("Box/VBox");
        if (layout != null)
            layout.AddThemeConstantOverride("separation", UiTheme.GapSection);

        // ── Title label ───────────────────────────────────────────────────────
        var title = GetNodeOrNull<Label>("Box/VBox/Title");
        if (title != null)
        {
            title.Text = GameStrings.Tr("SETTLEMENT_TITLE");
            UiTheme.Style(title, UiTheme.TypeRole.Heading);
            title.HorizontalAlignment = HorizontalAlignment.Center;
        }

        // ── Summary label ─────────────────────────────────────────────────────
        _summary = GetNodeOrNull<Label>("Box/VBox/Summary");
        if (_summary != null)
        {
            UiTheme.Style(_summary, UiTheme.TypeRole.Body);
            _summary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _summary.CustomMinimumSize = new Vector2(744, 150);
            _summary.HorizontalAlignment = HorizontalAlignment.Center;
            _summary.VerticalAlignment = VerticalAlignment.Center;
        }

        // ── Continue button ───────────────────────────────────────────────────
        var btn = GetNodeOrNull<Button>("Box/VBox/ContinueButton");
        if (btn != null)
        {
            btn.Text = string.IsNullOrEmpty(NextScenePath)
                ? "Return to Title"
                : ChapterRuntime.CurrentChapter == 1 ? "Enter Frostbound Trench"
                : ChapterRuntime.CurrentChapter == 2 ? "Go to the Silent Lighthouse"
                : "View the Restored Sea";
            btn.Pressed += OnContinuePressed;
            UiTheme.StyleButton(btn);
        }
    }

    public void ShowResult(string summary)
    {
        var dim = GetNodeOrNull<ColorRect>("Dim");
        if (dim != null) dim.Color = UiTheme.Scrim(0.85f).BgColor;
        Visible = true;
        if (_summary != null) _summary.Text = summary;
        GetTree().Paused = true;
        AudioManager.Instance?.PlaySfx("ui_confirm");
    }

    private void OnContinuePressed()
    {
        GetTree().Paused = false;
        AudioManager.Instance?.PlaySfx("ui_confirm");
        if (NextScenePath.EndsWith("game_scene2.tscn")) SaveManager.Instance?.SaveCampaignBoundary(2);
        else if (NextScenePath.EndsWith("game_scene3.tscn")) SaveManager.Instance?.SaveCampaignBoundary(3);
        GetTree().ChangeSceneToFile(string.IsNullOrEmpty(NextScenePath) ? TitleScenePath : NextScenePath);
    }
}

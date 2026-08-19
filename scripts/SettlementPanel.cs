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
        var box = GetNodeOrNull<Panel>("Box");
        if (box != null)
            box.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 24, pad: 36));

        // ── Title label ───────────────────────────────────────────────────────
        var title = GetNodeOrNull<Label>("Box/VBox/Title");
        if (title != null)
        {
            title.Text = GameStrings.Tr("SETTLEMENT_TITLE");
            UiTheme.ApplyFont(title, UiTheme.FontH1);
            title.AddThemeColorOverride("font_color", UiTheme.Accent);
        }

        // ── Summary label ─────────────────────────────────────────────────────
        _summary = GetNodeOrNull<Label>("Box/VBox/Summary");
        if (_summary != null)
        {
            UiTheme.ApplyFont(_summary, UiTheme.FontBody);
            _summary.AddThemeColorOverride("font_color", UiTheme.Ink);
            _summary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        }

        // ── Continue button ───────────────────────────────────────────────────
        var btn = GetNodeOrNull<Button>("Box/VBox/ContinueButton");
        if (btn != null)
        {
            btn.Text = string.IsNullOrEmpty(NextScenePath)
                ? "回到潮汐记录"
                : ChapterRuntime.CurrentChapter == 1 ? "进入霜骨海沟"
                : ChapterRuntime.CurrentChapter == 2 ? "前往断流灯塔"
                : "回望复苏的海";
            btn.Pressed += OnContinuePressed;
            UiTheme.StyleButton(btn, UiTheme.FontBody);
        }
    }

    public void ShowResult(string summary)
    {
        var dim = GetNodeOrNull<ColorRect>("Dim");
        if (dim != null) dim.Color = new Color(0.04f, 0.12f, 0.18f, 0.85f);
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

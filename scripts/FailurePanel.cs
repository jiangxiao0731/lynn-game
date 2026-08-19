using Godot;

namespace ShallowSeaDream;

/// res://scripts/FailurePanel.cs
/// Lose screen shown when player HP reaches 0. RestartButton (按 T 重新开始) +
/// QuitToTitleButton. Plays player_defeat.ogg on show.
public partial class FailurePanel : Control
{
    [Export] public string TitleScenePath = "res://scenes/title.tscn";

    public override void _Ready()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;

        // ── Deep-sea glass panel background ──────────────────────────────────
        var box = GetNodeOrNull<Panel>("Box");
        if (box != null)
            box.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 24, pad: 36));

        // ── Failure title label ───────────────────────────────────────────────
        var label = GetNodeOrNull<Label>("Box/VBox/FailureLabel");
        if (label != null)
        {
            label.Text = GameStrings.Tr("FAILURE_LABEL");
            UiTheme.ApplyFont(label, UiTheme.FontH1);
            label.AddThemeColorOverride("font_color", UiTheme.Accent);
        }

        // ── Restart hint label ────────────────────────────────────────────────
        var hint = GetNodeOrNull<Label>("Box/VBox/RestartHint");
        if (hint != null)
        {
            hint.Text = GameStrings.Tr("RESTART_HINT");
            UiTheme.ApplyFont(hint, UiTheme.FontSmall);
            hint.AddThemeColorOverride("font_color", UiTheme.InkDim);
        }

        // ── Restart button ────────────────────────────────────────────────────
        var restart = GetNodeOrNull<Button>("Box/VBox/RestartButton");
        if (restart != null)
        {
            restart.Pressed += OnRestartPressed;
            UiTheme.StyleButton(restart, UiTheme.FontBody);
        }

        // ── Quit-to-title button (secondary, non-primary colour) ──────────────
        var quit = GetNodeOrNull<Button>("Box/VBox/QuitToTitleButton");
        if (quit != null)
        {
            quit.Pressed += OnQuitToTitlePressed;
            UiTheme.StyleButton(quit, UiTheme.FontBody, primary: false);
        }
    }

    public void ShowFailure()
    {
        var dim = GetNodeOrNull<ColorRect>("Dim");
        if (dim != null) dim.Color = new Color(0.04f, 0.12f, 0.18f, 0.90f);
        Visible = true;
        GetTree().Paused = true;
        AudioManager.Instance?.PlaySfx("player_defeat");
    }

    private void OnRestartPressed()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void OnQuitToTitlePressed()
    {
        GetTree().Paused = false;
        SaveManager.Instance?.Reset();
        GetTree().ChangeSceneToFile(TitleScenePath);
    }
}

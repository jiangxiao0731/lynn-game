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
        var box = GetNodeOrNull<PanelContainer>("Box");
        if (box != null)
        {
            box.CustomMinimumSize = new Vector2(640, 390);
            box.AddThemeStyleboxOverride("panel", UiTheme.OverlayPanel(radius: 24, pad: UiTheme.PadScreen));
        }

        var layout = GetNodeOrNull<VBoxContainer>("Box/VBox");
        if (layout != null)
            layout.AddThemeConstantOverride("separation", UiTheme.GapSection);

        // ── Failure title label ───────────────────────────────────────────────
        var label = GetNodeOrNull<Label>("Box/VBox/FailureLabel");
        if (label != null)
        {
            label.Text = GameStrings.Tr("FAILURE_LABEL");
            UiTheme.Style(label, UiTheme.TypeRole.Heading);
            label.HorizontalAlignment = HorizontalAlignment.Center;
        }

        // ── Restart hint label ────────────────────────────────────────────────
        var hint = GetNodeOrNull<Label>("Box/VBox/RestartHint");
        if (hint != null)
        {
            hint.Text = GameStrings.Tr("RESTART_HINT");
            UiTheme.Style(hint, UiTheme.TypeRole.Meta);
            hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            hint.HorizontalAlignment = HorizontalAlignment.Center;
        }

        // ── Restart button ────────────────────────────────────────────────────
        var restart = GetNodeOrNull<Button>("Box/VBox/RestartButton");
        if (restart != null)
        {
            restart.Pressed += OnRestartPressed;
            UiTheme.StyleButton(restart);
        }

        // ── Quit-to-title button (secondary, non-primary colour) ──────────────
        var quit = GetNodeOrNull<Button>("Box/VBox/QuitToTitleButton");
        if (quit != null)
        {
            quit.Pressed += OnQuitToTitlePressed;
            UiTheme.StyleButton(quit, primary: false);
        }
    }

    public void ShowFailure()
    {
        var dim = GetNodeOrNull<ColorRect>("Dim");
        if (dim != null) dim.Color = UiTheme.Scrim(0.90f).BgColor;
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

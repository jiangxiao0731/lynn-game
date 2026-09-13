using Godot;

namespace ShallowSeaDream;

/// res://scripts/WindowManager.cs
/// Autoload. The project ships windowed at 1920x1080; this adds the fullscreen
/// toggle Godot does not provide out of the box. F11 works everywhere and
/// Cmd/Alt + Enter matches the platform habit. Escape is left alone: it is the
/// pause key.
public partial class WindowManager : Node
{
    public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

    /// Stretch aspect is "expand", so the screen is always filled with no bars. On a
    /// screen taller than 16:9 (a 16:10 MacBook) that shows more than 1080 world
    /// pixels vertically, past the painted map, so the camera zooms in just enough
    /// to keep the map filling the height. The HUD uses the whole screen either way.
    public override void _Process(double delta)
    {
        var camera = GetViewport().GetCamera2D();
        if (camera == null) return;
        float zoom = Mathf.Max(1f, GetViewport().GetVisibleRect().Size.Y / ChapterMap.Height);
        if (!Mathf.IsEqualApprox(camera.Zoom.X, zoom)) camera.Zoom = new Vector2(zoom, zoom);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        // Escape is deliberately NOT bound here: it is the pause key, and sharing it
        // made fullscreen and pause swallow each other's input.
        // macOS takes F11 for "Show Desktop", so Ctrl+Cmd+F (the system shortcut)
        // and Cmd+Enter are the keys that actually reach the game there.
        bool toggle = key.Keycode == Key.F11
            || (key.Keycode == Key.Enter && (key.AltPressed || key.MetaPressed))
            || (key.Keycode == Key.F && key.MetaPressed && key.CtrlPressed);
        if (!toggle) return;

        SetFullscreen(!IsFullscreen);
        GetViewport().SetInputAsHandled();
    }

    public static bool IsFullscreen =>
        DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen
            or DisplayServer.WindowMode.ExclusiveFullscreen;

    public static void SetFullscreen(bool on) =>
        DisplayServer.WindowSetMode(on
            ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.Windowed);
}

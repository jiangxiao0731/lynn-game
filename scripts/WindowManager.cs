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

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        // Escape is deliberately NOT bound here: it is the pause key, and sharing it
        // made fullscreen and pause swallow each other's input.
        bool toggle = key.Keycode == Key.F11
            || (key.Keycode == Key.Enter && (key.AltPressed || key.MetaPressed));
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

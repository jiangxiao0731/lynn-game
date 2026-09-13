using Godot;

namespace ShallowSeaDream;

/// res://scripts/PauseInput.cs
/// Autoload. The level controllers run in the default Pausable mode, so once
/// `GetTree().Paused` is true they stop receiving input and can never toggle the
/// pause back off — Escape locked the game up. This node always processes, so it
/// owns the un-pause half of the toggle no matter who paused the tree.
///
/// It deliberately only ever *clears* a pause. Entering pause stays with the level
/// controllers, which also play the sound and raise GamePaused; panels that pause
/// on purpose (log, settlement, failure) set ProcessMode.Always themselves and
/// handle Escape before it reaches here.
public partial class PauseInput : Node
{
    public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!GetTree().Paused) return;
        if (!@event.IsActionPressed("pause_game")) return;

        GetTree().Paused = false;
        Events.Instance?.EmitSignal(Events.SignalName.GamePaused, false);
        GetViewport().SetInputAsHandled();
    }
}

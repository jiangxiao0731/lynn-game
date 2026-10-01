using Godot;

/// Scene builder — run after dotnet build to produce res://scenes/ending.tscn.
public partial class BuildEnding : SceneBuilderBase
{
    public override void _Initialize()
    {
        var temp = new Node();
        var root = new Control { Name = "Ending" };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);
        root.SetScript(GD.Load("res://scripts/EndingController.cs"));

        var built = temp.GetChild(0);
        temp.RemoveChild(built);
        temp.Free();
        PackAndSave(built, "res://scenes/ending.tscn");
    }
}

using Godot;

/// Scene builder — run: dotnet build && timeout 60 godot --headless --script scenes/BuildFailurePanel.cs
/// Produces res://scenes/failure_panel.tscn (placeholder, no assets).
public partial class BuildFailurePanel : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: failure_panel");

        var temp = new Node();
        var root = new Control();
        root.Name = "FailurePanel";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);

        var bg = new ColorRect();
        bg.Name = "Dim";
        bg.Color = new Color(0.04f, 0.12f, 0.18f, 0.9f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(bg);

        var box = new PanelContainer();
        box.Name = "Box";
        box.SetAnchorsPreset(Control.LayoutPreset.Center);
        root.AddChild(box);

        var vbox = new VBoxContainer();
        vbox.Name = "VBox";
        box.AddChild(vbox);

        var label = new Label();
        label.Name = "FailureLabel";
        label.Text = "Shimmer Faded";
        vbox.AddChild(label);

        var hint = new Label();
        hint.Name = "RestartHint";
        hint.Text = "Press T to restart";
        vbox.AddChild(hint);

        var restart = new Button();
        restart.Name = "RestartButton";
        restart.Text = "Restart Chapter";
        vbox.AddChild(restart);

        var quit = new Button();
        quit.Name = "QuitToTitleButton";
        quit.Text = "Return to Title";
        vbox.AddChild(quit);

        root.SetScript(GD.Load("res://scripts/FailurePanel.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/failure_panel.tscn");
    }
}

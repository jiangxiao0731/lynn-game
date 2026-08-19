using Godot;

/// Scene builder — run: dotnet build && timeout 60 godot --headless --script scenes/BuildSettlementPanel.cs
/// Produces res://scenes/settlement_panel.tscn (placeholder, no assets).
public partial class BuildSettlementPanel : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: settlement_panel");

        var temp = new Node();
        var root = new Control();
        root.Name = "SettlementPanel";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);

        var bg = new ColorRect();
        bg.Name = "Dim";
        bg.Color = new Color(0.04f, 0.12f, 0.18f, 0.85f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(bg);

        var box = new PanelContainer();
        box.Name = "Box";
        box.SetAnchorsPreset(Control.LayoutPreset.Center);
        root.AddChild(box);

        var vbox = new VBoxContainer();
        vbox.Name = "VBox";
        box.AddChild(vbox);

        var title = new Label();
        title.Name = "Title";
        title.Text = "Area Restored";
        vbox.AddChild(title);

        var summary = new Label();
        summary.Name = "Summary";
        summary.Text = "";
        vbox.AddChild(summary);

        var button = new Button();
        button.Name = "ContinueButton";
        button.Text = "Continue";
        vbox.AddChild(button);

        root.SetScript(GD.Load("res://scripts/SettlementPanel.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/settlement_panel.tscn");
    }
}

using Godot;

/// Scene builder — run: dotnet build && timeout 60 godot --headless --script scenes/BuildTutorial.cs
/// Produces res://scenes/tutorial.tscn (3-page modal placeholder, no art assets).
public partial class BuildTutorial : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: tutorial");

        var temp = new Node();
        var root = new Control();
        root.Name = "Tutorial";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);

        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Color = new Color(0.04f, 0.12f, 0.18f, 1.0f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(bg);

        // Procedural jellyfish "stage" placeholder (Panel + glyph), tweened base→water at runtime.
        var stage = new Panel();
        stage.Name = "JellyfishStage";
        stage.SetAnchorsPreset(Control.LayoutPreset.Center);
        root.AddChild(stage);

        var glyph = new Label();
        glyph.Name = "Glyph";
        glyph.Text = "✿";
        stage.AddChild(glyph);

        var modal = new PanelContainer();
        modal.Name = "Modal";
        modal.SetAnchorsPreset(Control.LayoutPreset.Center);
        root.AddChild(modal);

        var vbox = new VBoxContainer();
        vbox.Name = "VBox";
        modal.AddChild(vbox);

        var pageTitle = new Label();
        pageTitle.Name = "PageTitle";
        pageTitle.Text = "Chapter 1 · Tidepool Nursery";
        vbox.AddChild(pageTitle);

        var pageBody = new Label();
        pageBody.Name = "PageBody";
        pageBody.Text = "";
        vbox.AddChild(pageBody);

        var next = new Button();
        next.Name = "NextButton";
        next.Text = "Next";
        vbox.AddChild(next);

        root.SetScript(GD.Load("res://scripts/TutorialController.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/tutorial.tscn");
    }
}

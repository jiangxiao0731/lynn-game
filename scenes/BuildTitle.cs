using Godot;

/// Scene builder — run: dotnet build && timeout 60 godot --headless --script scenes/BuildTitle.cs
/// Produces res://scenes/title.tscn (placeholder background, no art assets).
public partial class BuildTitle : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: title");

        var temp = new Node();
        var root = new Control();
        root.Name = "Title";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        temp.AddChild(root);

        var bg = new ColorRect();
        bg.Name = "Background";
        bg.Color = new Color(0.04f, 0.12f, 0.18f, 1.0f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(bg);

        var center = new CenterContainer();
        center.Name = "Center";
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var vbox = new VBoxContainer();
        vbox.Name = "Menu";
        center.AddChild(vbox);

        var titleLabel = new Label();
        titleLabel.Name = "GameTitle";
        titleLabel.Text = "潮汐微光";
        vbox.AddChild(titleLabel);

        var begin = new Button();
        begin.Name = "BeginButton";
        begin.Text = "潜入海洋";
        vbox.AddChild(begin);

        var newGame = new Button();
        newGame.Name = "NewGameButton";
        newGame.Text = "开始游戏";
        vbox.AddChild(newGame);

        var exit = new Button();
        exit.Name = "ExitButton";
        exit.Text = "返回岸上";
        vbox.AddChild(exit);

        root.SetScript(GD.Load("res://scripts/TitleController.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/title.tscn");
    }
}

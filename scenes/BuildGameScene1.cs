using Godot;

/// Scene builder — run LAST: dotnet build && timeout 60 godot --headless --script scenes/BuildGameScene1.cs
/// Produces res://scenes/game_scene1.tscn — Level 1 placeholder (no art assets).
/// Depends on: res://scenes/settlement_panel.tscn, res://scenes/failure_panel.tscn.
public partial class BuildGameScene1 : SceneBuilderBase
{
    public override void _Initialize()
    {
        GD.Print("Generating: game_scene1");

        var temp = new Node();
        var root = new Node2D();
        root.Name = "GameScene1";
        temp.AddChild(root);

        // --- Deep-sea background placeholder ---
        var bgLayer = new CanvasLayer();
        bgLayer.Name = "BackgroundLayer";
        bgLayer.Layer = -10;
        root.AddChild(bgLayer);

        var bg = new ColorRect();
        bg.Name = "DeepSea";
        bg.Color = new Color(0.04f, 0.12f, 0.18f, 1.0f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bgLayer.AddChild(bg);

        // --- Map + wall layout placeholder ---
        var map = new Node2D();
        map.Name = "Map";
        root.AddChild(map);

        var walls = new StaticBody2D();
        walls.Name = "Walls";
        walls.CollisionLayer = 32; // layer 6 "walls"
        map.AddChild(walls);

        // --- Player (with form sprites + camera) ---
        var player = new CharacterBody2D();
        player.Name = "Player";
        player.AddToGroup("player");
        root.AddChild(player);

        AddFormSprite(player, "AnimatedSprite2D");
        AddFormSprite(player, "WaterSprite2D");
        AddFormSprite(player, "IceSprite2D");
        AddFormSprite(player, "ElectricSprite2D");

        var playerArea = new Area2D();
        playerArea.Name = "InteractArea";
        player.AddChild(playerArea);

        var camera = new Camera2D();
        camera.Name = "Camera2D";
        player.AddChild(camera);

        // --- Boss ---
        var boss = new CharacterBody2D();
        boss.Name = "BroodMother";
        boss.Position = new Vector2(900, 400);
        boss.AddToGroup("monster");
        root.AddChild(boss);

        // --- Barrel + NPCs (placeholder nodes) ---
        var barrel = new StaticBody2D();
        barrel.Name = "GasolineBarrel";
        barrel.AddToGroup("box");
        root.AddChild(barrel);

        var npc = new Node2D();
        npc.Name = "GrannyLan";
        npc.AddToGroup("npc");
        root.AddChild(npc);

        var starfish = new Node2D();
        starfish.Name = "StarfishNPC";
        starfish.AddToGroup("flavor_npc");
        root.AddChild(starfish);

        var seaweed = new Node2D();
        seaweed.Name = "SeaweedNPC";
        seaweed.AddToGroup("flavor_npc");
        root.AddChild(seaweed);

        // --- Subsystem nodes (god-class split) ---
        var skillSystem = new Node();
        skillSystem.Name = "SkillSystem";
        root.AddChild(skillSystem);

        var objectiveManager = new Node();
        objectiveManager.Name = "ObjectiveManager";
        root.AddChild(objectiveManager);

        var elementSpawner = new Node2D();
        elementSpawner.Name = "ElementSpawner";
        root.AddChild(elementSpawner);

        // --- HUD ---
        var hud = new CanvasLayer();
        hud.Name = "HUD";
        hud.Layer = 10;
        root.AddChild(hud);

        // Panels instanced from leaf scenes (built earlier).
        var settlement = GD.Load<PackedScene>("res://scenes/settlement_panel.tscn").Instantiate();
        settlement.Name = "SettlementPanel";
        hud.AddChild(settlement);

        var failure = GD.Load<PackedScene>("res://scenes/failure_panel.tscn").Instantiate();
        failure.Name = "FailurePanel";
        hud.AddChild(failure);

        // --- Scripts LAST ---
        boss.SetScript(GD.Load("res://scripts/BossController.cs"));
        player.SetScript(GD.Load("res://scripts/Player.cs"));
        skillSystem.SetScript(GD.Load("res://scripts/SkillSystem.cs"));
        objectiveManager.SetScript(GD.Load("res://scripts/ObjectiveManager.cs"));
        elementSpawner.SetScript(GD.Load("res://scripts/ElementSpawner.cs"));
        hud.SetScript(GD.Load("res://scripts/HudController.cs"));
        root.SetScript(GD.Load("res://scripts/GameSceneController.cs"));

        var rootNode = temp.GetChild(0);
        temp.RemoveChild(rootNode);
        temp.Free();

        PackAndSave(rootNode, "res://scenes/game_scene1.tscn");
    }

    private static void AddFormSprite(Node parent, string name)
    {
        var sprite = new AnimatedSprite2D();
        sprite.Name = name;
        sprite.Visible = name == "AnimatedSprite2D"; // base form visible by default
        parent.AddChild(sprite);
    }
}

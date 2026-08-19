using System;
using System.Collections.Generic;
using Godot;

namespace ShallowSeaDream;

/// Shared authored runtime for Chapters 2 and 3. The two thin scenes select a
/// chapter id; this controller builds a distinct maze, ecological interactions,
/// elemental pickups, restoration layers, story beats and chapter transition.
public partial class ChapterLevelController : Node2D
{
    [Export(PropertyHint.Range, "2,3,1")] public int ChapterId = 2;

    private const float MapWidth = 3600f;
    private const float MapHeight = 1080f;
    private const int RequiredFragments = 8;
    private const float InteractionRange = 210f;

    private Player _player = null!;
    private SkillSystem _skills = null!;
    private BossController _boss = null!;
    private DialogueRunner _dialogue = null!;
    private SettlementPanel? _settlement;
    private ObjectiveStage _stage = ObjectiveStage.FindNpc;
    private int _fragments;
    private int _restoredNodes;
    private int _zone = -1;
    private bool _guardianBeatPlayed;
    private bool _completed;
    private bool _smokeMode;
    private float _time;

    private Node2D _guide = null!;
    private Label _guidePrompt = null!;
    private Sprite2D _exit = null!;
    private StaticBody2D? _arenaGate;
    private readonly List<Node2D> _restoreNodes = new();
    private readonly List<Label> _restorePrompts = new();
    private readonly List<StaticBody2D> _shortcutGates = new();
    private readonly List<Sprite2D> _fragmentsInWorld = new();
    private readonly Dictionary<Sprite2D, Vector2> _fragmentOrigins = new();
    private readonly List<ColorRect> _veils = new();
    private readonly List<Line2D> _energyLines = new();

    private ElementForm RequiredForm => ChapterId == 2 ? ElementForm.Ice : ElementForm.Electric;
    private string GuideTimeline => ChapterId == 2 ? NarrativeData.Chapter2Opening : NarrativeData.Chapter3Opening;
    private string GuardianTimeline => ChapterId == 2 ? NarrativeData.Chapter2Guardian : NarrativeData.Chapter3Guardian;
    private string EndingTimeline => ChapterId == 2 ? NarrativeData.Chapter2Ending : NarrativeData.Chapter3Ending;

    public override void _EnterTree() => ChapterRuntime.SetChapter(ChapterId);

    public override void _Ready()
    {
        _player = GetNode<Player>("Player");
        _skills = GetNode<SkillSystem>("SkillSystem");
        _boss = GetNode<BossController>("BroodMother");
        _settlement = GetNodeOrNull<SettlementPanel>("HUD/SettlementPanel");

        BuildWorld();
        _dialogue = new DialogueRunner { Name = "DialogueRunner" };
        AddChild(_dialogue);

        var bus = Events.Instance;
        if (bus != null)
        {
            bus.BossDefeated += OnGuardianRestored;
            bus.BossHealthChanged += OnGuardianHealthChanged;
        }

        AudioManager.Instance?.PlayMusic("level1_underwater_ambient");
        _player.SetSafePoint(_player.GlobalPosition);
        _player.BroadcastHealth();
        BroadcastObjective();
        Events.Instance?.EmitSignal(Events.SignalName.ZoneEntered, ChapterRuntime.Zones[0]);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            ChapterId == 2 ? "第二章 · 霜骨海沟　靠近灯笼鱼·盏并按 E" : "第三章 · 断流灯塔　靠近迷途鱼群并按 E");

        RestoreProgressIfSaved();
        _smokeMode = Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-complete") >= 0;
        if (_smokeMode)
            CallDeferred(nameof(RunSmokeComplete));
        else if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--preview-guardian") >= 0)
            CallDeferred(nameof(PreviewGuardian));
    }

    private void BuildWorld()
    {
        var map = GetNode<Node2D>("Map");
        BuildBackdrop(map);
        BuildBoundsAndMaze(map);
        BuildGuide();
        BuildRestoreNodes(map);
        BuildFragments(map);
        BuildExit(map);
    }

    private void BuildBackdrop(Node2D map)
    {
        string[] sources = { AssetLoader.ZoneBackground(NarrativeData.ZoneShallows), AssetLoader.ZoneBackground(NarrativeData.ZoneSediment), AssetLoader.ZoneBackground(NarrativeData.ZoneDepths) };
        for (int i = 0; i < 3; i++)
        {
            float x0 = i * 1200f;
            var texture = AssetLoader.Texture(sources[i]);
            if (texture != null)
            {
                var art = new TextureRect
                {
                    Name = $"ChapterArt{i + 1}", Position = new Vector2(x0, 0), Size = new Vector2(1200, MapHeight),
                    Texture = texture, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.Scale, ZIndex = -100,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    Modulate = ChapterId == 2
                        ? new Color(0.72f, 0.88f, 1.08f, 1f)
                        : new Color(0.72f + i * 0.08f, 0.66f + i * 0.06f, 0.88f, 1f),
                };
                map.AddChild(art);
            }

            var veil = new ColorRect
            {
                Name = $"DamageVeil{i + 1}", Position = new Vector2(x0, 0), Size = new Vector2(1200, MapHeight),
                Color = ChapterId == 2
                    ? new Color(0.12f, 0.30f, 0.48f, 0.34f - i * 0.03f)
                    : new Color(0.16f, 0.08f, 0.08f, 0.42f - i * 0.03f),
                ZIndex = -96, MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            map.AddChild(veil);
            _veils.Add(veil);

            var zoneName = UiTheme.MakeLabel(ChapterRuntime.Zones[i], 34,
                ChapterId == 2 ? new Color(0.72f, 0.92f, 1f, 0.72f) : new Color(0.46f, 0.94f, 0.78f, 0.72f));
            zoneName.Position = new Vector2(x0 + 76, 930);
            zoneName.ZIndex = -10;
            map.AddChild(zoneName);

            var flow = new Line2D
            {
                Name = $"RestorationCurrent{i + 1}", Width = ChapterId == 2 ? 14f : 8f,
                DefaultColor = ChapterId == 2 ? new Color(0.55f, 0.96f, 1f, 0.08f) : new Color(0.42f, 0.92f, 0.74f, 0.08f),
                ZIndex = -20, Antialiased = true,
            };
            var anchors = ChapterId == 2
                ? new[] { new Vector2(x0 + 80, 820), new Vector2(x0 + 360, 710), new Vector2(x0 + 640, 790), new Vector2(x0 + 1110, 650) }
                : new[] { new Vector2(x0 + 90, 230), new Vector2(x0 + 390, 420), new Vector2(x0 + 710, 280), new Vector2(x0 + 1110, 510) };
            flow.Points = HandDrawnPath(anchors, ChapterId * 17 + i);
            map.AddChild(flow);
            _energyLines.Add(flow);
        }
        AddCurrentSeam(map, 1200f);
        AddCurrentSeam(map, 2400f);
    }

    private void BuildBoundsAndMaze(Node2D map)
    {
        var walls = GetNode<StaticBody2D>("Map/Walls");
        AddWall(walls, new Rect2(0, 0, MapWidth, 24));
        AddWall(walls, new Rect2(0, MapHeight - 24, MapWidth, 24));
        AddWall(walls, new Rect2(0, 0, 24, MapHeight));
        AddWall(walls, new Rect2(MapWidth - 24, 0, 24, MapHeight));

        Rect2[] iceMaze =
        {
            new(410,70,100,330), new(410,650,100,360), new(650,460,330,90), new(1040,90,90,360), new(980,720,300,90),
            new(1390,70,95,350), new(1290,570,380,90), new(1790,300,95,500), new(2010,130,350,90), new(2040,690,340,90), new(2330,330,80,320),
            new(2500,250,350,90), new(2700,520,90,430), new(2990,70,90,330), new(2920,720,390,90), new(3300,170,90,310),
        };
        Rect2[] electricMaze =
        {
            new(320,300,360,90), new(620,620,90,370), new(800,100,90,360), new(1040,520,260,90), new(1080,760,90,260),
            new(1320,170,380,90), new(1510,440,90,430), new(1790,700,390,90), new(2000,70,90,360), new(2240,430,190,90), new(2320,650,90,350),
            new(2520,90,90,390), new(2700,650,360,90), new(2950,280,90,360), new(3200,90,90,330), new(3320,600,230,90),
        };
        var layout = ChapterId == 2 ? iceMaze : electricMaze;
        int seed = ChapterId * 41;
        foreach (var rect in layout)
        {
            AddWall(walls, rect);
            AddReefVisual(map, rect, seed++);
        }

        // The final arena has one readable entrance; the elemental collection opens it.
        AddWall(walls, new Rect2(2840, 24, 72, 320));
        AddWall(walls, new Rect2(2840, 736, 72, 320));
        AddReefVisual(map, new Rect2(2840, 24, 72, 320), seed++);
        AddReefVisual(map, new Rect2(2840, 736, 72, 320), seed++);
        _arenaGate = MakeGate(map, new Rect2(2840, 344, 72, 392), "GuardianGate", ChapterId == 2 ? Palette.ElementIce : Palette.ElementElectric);
    }

    private void BuildGuide()
    {
        bool ice = ChapterId == 2;
        _guide = new Node2D { Name = ice ? "LanternNPC" : "ShoalNPC", Position = ice ? new Vector2(330, 530) : new Vector2(340, 650) };
        AddChild(_guide);
        var halo = CircleLine(82f, ice ? new Color(0.52f, 0.94f, 1f, 0.72f) : new Color(0.42f, 0.94f, 0.76f, 0.78f), 5f);
        _guide.AddChild(halo);
        var sprite = new Sprite2D { Texture = AssetLoader.Texture(AssetLoader.NpcPortrait(ice ? "lantern" : "shoal")), ZIndex = 1 };
        if (sprite.Texture != null) PlaceholderArt.FitSprite(sprite, 152f);
        _guide.AddChild(sprite);
        var name = WorldLabel(ice ? "灯笼鱼·盏  ·  解冻向导" : "迷途鱼群  ·  回路守望者", new Vector2(-145, 96), 290, UiTheme.Accent);
        _guide.AddChild(name);
        _guidePrompt = WorldLabel("E  ·  交谈", new Vector2(-75, 132), 150, UiTheme.Ink);
        _guidePrompt.Visible = false;
        _guide.AddChild(_guidePrompt);
        StartBob(_guide, 8f, 1.7f);
    }

    private void BuildRestoreNodes(Node2D map)
    {
        Vector2[] positions = ChapterId == 2
            ? new[] { new Vector2(900, 220), new Vector2(1680, 880), new Vector2(2420, 230) }
            : new[] { new Vector2(980, 850), new Vector2(1850, 250), new Vector2(2590, 850) };
        for (int i = 0; i < positions.Length; i++)
        {
            var node = new Node2D { Name = ChapterId == 2 ? $"ThawAnchor{i + 1}" : $"Relay{i + 1}", Position = positions[i] };
            map.AddChild(node);
            node.AddChild(CircleLine(64f, ChapterId == 2 ? new Color(0.62f, 0.90f, 1f, 0.62f) : new Color(0.38f, 0.90f, 0.70f, 0.72f), 6f));
            var icon = new Sprite2D
            {
                Texture = AssetLoader.Texture(AssetLoader.MemoryIcon) ?? PlaceholderArt.RoundBlob(64, Palette.ForForm(RequiredForm)),
                Modulate = ChapterId == 2 ? new Color(0.65f, 0.88f, 1f, 0.62f) : new Color(0.44f, 0.82f, 0.68f, 0.62f),
            };
            PlaceholderArt.FitSprite(icon, 88f);
            node.AddChild(icon);
            node.AddChild(WorldLabel(ChapterId == 2 ? $"解冻锚 {i + 1}" : $"潮汐继电器 {i + 1}", new Vector2(-110, 78), 220, UiTheme.InkDim));
            var prompt = WorldLabel("E  ·  唤醒", new Vector2(-75, 112), 150, UiTheme.Ink);
            prompt.Visible = false;
            node.AddChild(prompt);
            _restoreNodes.Add(node);
            _restorePrompts.Add(prompt);

            var gate = MakeGate(map,
                i == 0 ? new Rect2(1130, 450, 70, 180) : i == 1 ? new Rect2(2390, 430, 70, 180) : new Rect2(2710, 430, 70, 180),
                $"ShortcutGate{i + 1}", ChapterId == 2 ? Palette.ElementIce : Palette.PollutedTeal);
            _shortcutGates.Add(gate);
        }
    }

    private void BuildFragments(Node2D map)
    {
        Vector2[] ice = { new(210,210), new(600,870), new(740,230), new(1170,650), new(1370,480), new(1730,170), new(1960,900), new(2220,430), new(2500,900), new(2670,180), new(3010,900), new(3370,850) };
        Vector2[] electric = { new(190,540), new(520,190), new(740,520), new(1080,220), new(1280,850), new(1700,560), new(1940,900), new(2190,250), new(2470,560), new(2780,180), new(3150,860), new(3440,470) };
        foreach (var point in ChapterId == 2 ? ice : electric)
        {
            var shard = new Sprite2D
            {
                Name = ChapterId == 2 ? "IceFragment" : "ElectricSpark",
                Texture = AssetLoader.Texture(AssetLoader.ShardIcon) ?? PlaceholderArt.RoundBlob(56, Palette.ForForm(RequiredForm)),
                Position = point, Modulate = Palette.ForForm(RequiredForm), ZIndex = 3,
            };
            PlaceholderArt.FitSprite(shard, 68f);
            map.AddChild(shard);
            _fragmentsInWorld.Add(shard);
            _fragmentOrigins[shard] = point;
        }
    }

    private void BuildExit(Node2D map)
    {
        _exit = new Sprite2D
        {
            Name = "ChapterExit", Texture = AssetLoader.Texture(AssetLoader.MemoryIcon) ?? PlaceholderArt.RoundBlob(72, UiTheme.Accent),
            Position = new Vector2(3420, 540), Modulate = new Color(0.7f, 0.9f, 1f, 0.2f), ZIndex = -4,
        };
        PlaceholderArt.FitSprite(_exit, 112f);
        map.AddChild(_exit);
        StartBob(_exit, 10f, 1.25f);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        UpdateZone();
        UpdatePrompts();
        UpdateFragments();
        if (_stage == ObjectiveStage.DefeatBoss && !_guardianBeatPlayed && _player.GlobalPosition.DistanceTo(_boss.GlobalPosition) < 520f)
        {
            _guardianBeatPlayed = true;
            _dialogue.Play(GuardianTimeline);
        }
        if (_stage == ObjectiveStage.ExitLevel && _player.GlobalPosition.DistanceTo(_exit.GlobalPosition) <= GameConstants.ExitReachDistance + 30f)
            CompleteChapter();
    }

    private void UpdateZone()
    {
        int next = Mathf.Clamp(Mathf.FloorToInt(_player.GlobalPosition.X / 1200f), 0, 2);
        if (next == _zone) return;
        _zone = next;
        _player.SetSafePoint(_player.GlobalPosition);
        Events.Instance?.EmitSignal(Events.SignalName.ZoneEntered, ChapterRuntime.Zones[next]);
    }

    private void UpdatePrompts()
    {
        bool dialogueFree = _dialogue != null && !_dialogue.IsActive;
        _guidePrompt.Visible = dialogueFree && _stage == ObjectiveStage.FindNpc && Near(_guide);
        for (int i = 0; i < _restoreNodes.Count; i++)
            _restorePrompts[i].Visible = dialogueFree && IsNodeAvailable(i) && Near(_restoreNodes[i]);
    }

    private void UpdateFragments()
    {
        for (int i = _fragmentsInWorld.Count - 1; i >= 0; i--)
        {
            var shard = _fragmentsInWorld[i];
            if (!IsInstanceValid(shard)) { _fragmentsInWorld.RemoveAt(i); continue; }
            var origin = _fragmentOrigins[shard];
            shard.Position = origin + new Vector2(0, Mathf.Sin(_time * 2.2f + i * 0.7f) * 9f);
            shard.Modulate = new Color(shard.Modulate.R, shard.Modulate.G, shard.Modulate.B,
                _stage == ObjectiveStage.CollectShards ? 1f : 0.34f);
            if (_stage == ObjectiveStage.CollectShards && shard.GlobalPosition.DistanceTo(_player.GlobalPosition) <= GameConstants.ElementPickupDistance)
                CollectFragment(shard);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause_game"))
        {
            GetTree().Paused = !GetTree().Paused;
            Events.Instance?.EmitSignal(Events.SignalName.GamePaused, GetTree().Paused);
            return;
        }
        if (@event.IsActionPressed("restart_level"))
        {
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
            return;
        }
        if (!@event.IsActionPressed("e") || _dialogue.IsActive) return;
        if (_stage == ObjectiveStage.FindNpc && Near(_guide))
        {
            _dialogue.Play(GuideTimeline, () => Advance(ObjectiveStage.TalkStarfish));
            GetViewport().SetInputAsHandled();
            return;
        }
        for (int i = 0; i < _restoreNodes.Count; i++)
        {
            if (IsNodeAvailable(i) && Near(_restoreNodes[i]))
            {
                RestoreNode(i);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
    }

    private bool IsNodeAvailable(int index) => index == _restoredNodes &&
        ((index == 0 && _stage == ObjectiveStage.TalkStarfish) ||
         (index > 0 && _stage == ObjectiveStage.TalkSeaweed));

    private void RestoreNode(int index)
    {
        // Restoration is sequential so the maze path and visual story remain legible.
        if (index != _restoredNodes) return;
        _restoredNodes++;
        var icon = _restoreNodes[index].GetChildOrNull<Sprite2D>(1);
        if (icon != null)
        {
            icon.Modulate = ChapterId == 2 ? new Color(0.74f, 1f, 1f, 1f) : new Color(0.60f, 1f, 0.82f, 1f);
            icon.CreateTween().TweenProperty(icon, "scale", icon.Scale * 1.22f, 0.22f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
        if (index < _shortcutGates.Count && IsInstanceValid(_shortcutGates[index]))
        {
            _shortcutGates[index].CollisionLayer = 0;
            _shortcutGates[index].CreateTween().TweenProperty(_shortcutGates[index], "modulate:a", 0f, 0.5f)
                .Finished += _shortcutGates[index].QueueFree;
        }
        SetZoneRestored(index);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            ChapterId == 2 ? $"解冻锚 {index + 1} 已苏醒，冻结捷径正在融开。" : $"继电器 {index + 1} 已接通，安全回路向前延伸。" );
        AudioManager.Instance?.PlaySfx("objective_advance");
        if (index == 0) Advance(ObjectiveStage.TalkSeaweed);
        else if (_restoredNodes >= 3) Advance(ObjectiveStage.CollectShards);
    }

    private void SetZoneRestored(int index)
    {
        if (index < _veils.Count)
            _veils[index].CreateTween().TweenProperty(_veils[index], "color:a", 0.06f, 0.85f);
        if (index < _energyLines.Count)
        {
            var c = _energyLines[index].DefaultColor; c.A = 0.82f;
            _energyLines[index].CreateTween().TweenProperty(_energyLines[index], "default_color", c, 0.8f);
        }
    }

    private void CollectFragment(Sprite2D shard)
    {
        _fragmentsInWorld.Remove(shard);
        _fragmentOrigins.Remove(shard);
        _fragments++;
        _skills.AddCharge(RequiredForm);
        _player.SetForm(RequiredForm);
        Events.Instance?.EmitSignal(Events.SignalName.ElementPickedUp, (int)RequiredForm);
        Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, _fragments, RequiredFragments);
        AudioManager.Instance?.PlaySfx("pickup_water");
        shard.CreateTween().TweenProperty(shard, "scale", shard.Scale * 1.6f, 0.18f)
            .Finished += shard.QueueFree;
        if (_fragments >= RequiredFragments)
        {
            Advance(ObjectiveStage.DefeatBoss);
            if (_arenaGate != null && IsInstanceValid(_arenaGate))
            {
                _arenaGate.CollisionLayer = 0;
                _arenaGate.CreateTween().TweenProperty(_arenaGate, "modulate:a", 0f, 0.65f).Finished += _arenaGate.QueueFree;
            }
            Events.Instance?.EmitSignal(Events.SignalName.BossHealthChanged, _boss.CurrentHealth, _boss.MaxHealthValue);
        }
    }

    private void OnGuardianHealthChanged(int current, int max)
    {
        if (_stage == ObjectiveStage.DefeatBoss && current > 0 && current <= max / 2)
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, "它的外壳正在松开；继续用正确元素引导污染离开。" );
    }

    private void OnGuardianRestored()
    {
        if (_stage != ObjectiveStage.DefeatBoss) return;
        for (int i = 0; i < 3; i++) SetZoneRestored(i);
        _exit.CreateTween().TweenProperty(_exit, "modulate", Colors.White, 0.9f);
        _dialogue.Play(EndingTimeline, () => Advance(ObjectiveStage.ExitLevel));
    }

    private void CompleteChapter()
    {
        if (_completed) return;
        _completed = true;
        Advance(ObjectiveStage.Complete);
        if (_smokeMode) return;
        PersistProgress();
        _settlement?.ShowResult(ChapterId == 2
            ? "三座解冻锚重新推动潮水，霜壳守望者终于卸下冰甲。\n微光带着电元素的潮汐频率，继续前往断流灯塔。"
            : "三段安全回路重新连成海底星河，废热炉心转为温暖的珊瑚孵化场。\n海洋不会因一次净化永远安全，但修复可以从每一次选择开始。" );
    }

    private void Advance(ObjectiveStage next)
    {
        _stage = next;
        Events.Instance?.EmitSignal(Events.SignalName.ObjectiveAdvanced, (int)next);
        if (next == ObjectiveStage.CollectShards)
            Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, _fragments, RequiredFragments);
        if (next == ObjectiveStage.Complete)
            Events.Instance?.EmitSignal(Events.SignalName.ObjectiveCompleted);
        else AudioManager.Instance?.PlaySfx("objective_advance");
    }

    private void BroadcastObjective()
    {
        Events.Instance?.EmitSignal(Events.SignalName.ObjectiveAdvanced, (int)_stage);
        Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, _fragments, RequiredFragments);
    }

    private bool Near(Node2D node) => _player.GlobalPosition.DistanceTo(node.GlobalPosition) <= InteractionRange;

    private void PersistProgress()
    {
        const int boundaryLevel = 3;
        var boundaryStage = ChapterId == 2 ? ObjectiveStage.FindNpc : ObjectiveStage.Complete;
        var state = new SaveState(SaveManager.SaveVersion, Time.GetDatetimeStringFromSystem(true), _player.CurrentForm,
            _player.CurrentHealth, 0, 0, 0, boundaryStage, null, true, true, boundaryLevel, 0);
        SaveManager.Instance?.SaveState(state);
    }

    private void RestoreProgressIfSaved()
    {
        var state = SaveManager.Instance?.LoadState();
        if (state == null || state.CurrentLevel != ChapterId || state.ObjectiveStage == ObjectiveStage.Complete) return;
        // Chapter interaction topology is short; resume safely at the start rather
        // than restoring half-removed runtime gates without their authored visuals.
        _player.RestoreState(state.CurrentHealth, state.CurrentForm);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint, "已恢复本章角色状态；生态修复路线从入口重新确认。" );
    }

    private void RunSmokeComplete()
    {
        Advance(ObjectiveStage.TalkStarfish);
        RestoreNode(0); RestoreNode(1); RestoreNode(2);
        var copy = _fragmentsInWorld.ToArray();
        for (int i = 0; i < RequiredFragments && i < copy.Length; i++) CollectFragment(copy[i]);
        _guardianBeatPlayed = true;
        _boss.ApplyDamage(_boss.MaxHealthValue);
        Advance(ObjectiveStage.ExitLevel);
        CompleteChapter();
        GD.Print($"CHAPTER_SMOKE_OK level={ChapterId} stage={_stage} fragments={_fragments} nodes={_restoredNodes}");
    }

    private void PreviewGuardian()
    {
        _guardianBeatPlayed = true;
        _fragments = RequiredFragments;
        for (int i = 0; i < 3; i++) SetZoneRestored(i);
        _arenaGate?.QueueFree();
        _player.GlobalPosition = new Vector2(2980, 540);
        for (int i = 0; i < 5; i++) _skills.AddCharge(RequiredForm);
        _player.SetForm(RequiredForm);
        Advance(ObjectiveStage.DefeatBoss);
        Events.Instance?.EmitSignal(Events.SignalName.BossHealthChanged, _boss.CurrentHealth, _boss.MaxHealthValue);
    }

    private static void AddWall(StaticBody2D body, Rect2 rect)
    {
        body.AddChild(new CollisionShape2D
        {
            Position = rect.GetCenter(), Shape = new RectangleShape2D { Size = rect.Size },
        });
    }

    private void AddReefVisual(Node2D map, Rect2 rect, int seed)
    {
        float j = 10f + seed % 9;
        var points = new[]
        {
            rect.Position + new Vector2(j, 0), rect.Position + new Vector2(rect.Size.X * .42f, j * .4f),
            rect.Position + new Vector2(rect.Size.X - j * .4f, 0), rect.Position + new Vector2(rect.Size.X, rect.Size.Y * .36f),
            rect.End - new Vector2(j * .3f, 0), rect.Position + new Vector2(rect.Size.X * .58f, rect.Size.Y - j * .45f),
            rect.End - new Vector2(rect.Size.X - j * .3f, 0), rect.Position + new Vector2(0, rect.Size.Y * .58f),
        };
        var source = AssetLoader.Texture(AssetLoader.WallTile);
        var uv = new Vector2[points.Length];
        var uvOffset = new Vector2((seed * 83) % 420, (seed * 47) % 380);
        for (int i = 0; i < points.Length; i++) uv[i] = points[i] + uvOffset;
        var poly = new Polygon2D
        {
            Polygon = points,
            UV = uv,
            Texture = source,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
            Color = ChapterId == 2 ? new Color(0.62f, 0.78f, 0.88f, 0.88f) : new Color(0.48f, 0.66f, 0.62f, 0.92f),
            ZIndex = -40,
        };
        map.AddChild(poly);
    }

    private StaticBody2D MakeGate(Node2D map, Rect2 rect, string name, Color color)
    {
        var gate = new StaticBody2D { Name = name, Position = rect.GetCenter(), CollisionLayer = 32, ZIndex = -30 };
        gate.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = rect.Size } });
        var visual = new Polygon2D
        {
            Polygon = new[] { new Vector2(-rect.Size.X/2,-rect.Size.Y/2), new Vector2(rect.Size.X/2,-rect.Size.Y/2), new Vector2(rect.Size.X/2,rect.Size.Y/2), new Vector2(-rect.Size.X/2,rect.Size.Y/2) },
            Color = new Color(color.R, color.G, color.B, .05f),
        };
        gate.AddChild(visual);
        for (int i = 0; i < 4; i++)
        {
            float x = Mathf.Lerp(-rect.Size.X * .42f, rect.Size.X * .42f, i / 3f);
            gate.AddChild(new Line2D
            {
                Points = new[] { new Vector2(x, -rect.Size.Y/2), new Vector2(x + (i%2==0?10:-10), 0), new Vector2(x, rect.Size.Y/2) },
                Width = 4f, Antialiased = true, DefaultColor = new Color(color.R, color.G, color.B, .76f),
            });
        }
        map.AddChild(gate);
        return gate;
    }

    private void AddCurrentSeam(Node2D map, float x)
    {
        map.AddChild(new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(x - 55, 0), new Vector2(x + 42, 0), new Vector2(x + 24, 250),
                new Vector2(x + 60, 520), new Vector2(x + 18, 790), new Vector2(x + 48, MapHeight),
                new Vector2(x - 50, MapHeight), new Vector2(x - 22, 760), new Vector2(x - 64, 480), new Vector2(x - 26, 210),
            },
            Color = new Color(.02f,.09f,.13f,.24f), ZIndex = -95,
        });
    }

    private static Line2D CircleLine(float radius, Color color, float width)
    {
        var line = new Line2D { Width = width, DefaultColor = color, Closed = true, Antialiased = true, ZIndex = 0 };
        var points = new Vector2[32];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / points.Length;
            float wobble = Mathf.Sin(angle * 5f + radius) * 2.8f + Mathf.Sin(angle * 9f) * 1.4f;
            points[i] = Vector2.FromAngle(angle) * (radius + wobble);
        }
        line.Points = points;
        return line;
    }

    private static Vector2[] HandDrawnPath(Vector2[] anchors, int seed)
    {
        var points = new List<Vector2> { anchors[0] };
        for (int a = 0; a < anchors.Length - 1; a++)
        {
            Vector2 from = anchors[a], to = anchors[a + 1];
            Vector2 normal = (to - from).Normalized().Orthogonal();
            for (int step = 1; step <= 5; step++)
            {
                float t = step / 5f;
                float inkWobble = Mathf.Sin((a * 5 + step) * 1.73f + seed) * 7f
                                  + Mathf.Sin((a + step) * .91f + seed * .4f) * 3f;
                points.Add(from.Lerp(to, t) + normal * inkWobble * Mathf.Sin(Mathf.Pi * t));
            }
        }
        return points.ToArray();
    }

    private static Label WorldLabel(string text, Vector2 position, float width, Color color)
    {
        var label = UiTheme.MakeLabel(text, UiTheme.FontSmall, color);
        label.Position = position; label.Size = new Vector2(width, 32); label.HorizontalAlignment = HorizontalAlignment.Center; label.ZIndex = 4;
        label.AddThemeColorOverride("font_shadow_color", new Color(0,0,0,.85f)); label.AddThemeConstantOverride("shadow_offset_x", 2); label.AddThemeConstantOverride("shadow_offset_y", 2);
        return label;
    }

    private static void StartBob(Node2D node, float amount, float seconds)
    {
        Vector2 rest = node.Position;
        var tween = node.CreateTween().SetLoops();
        tween.TweenProperty(node, "position", rest + new Vector2(0, -amount), seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(node, "position", rest + new Vector2(0, amount), seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}

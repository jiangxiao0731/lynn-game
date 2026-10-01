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

    /// Length of the chapter, taken from its three paintings at their own aspect
    /// ratio. Hand-authored X coordinates below are still written against the old
    /// 3600 layout and are restretched through SX / SR.
    private float MapWidth => ChapterMap.TotalWidth(ChapterId);
    private const float MapHeight = 1080f;
    private const int RequiredFragments = 8;
    private const float InteractionRange = 210f;

    /// The pickup is the chapter's pollution item, so it reads as an object rather
    /// than a mote, while staying smaller than the smallest resident (126px).
    private const float FragmentDisplaySize = 96f;

    /// Keep a fragment this far from any resident or objective node centre.
    private const float FragmentClearance = 190f;

    /// Stretch an authored point / rect across the long map.
    private Vector2 SX(Vector2 authored) => ChapterMap.Place(ChapterId, authored);
    private Rect2 SR(Rect2 authored) => ChapterMap.Place(ChapterId, authored);
    /// A slice of the arena mouth, anchored to the map's right end.
    private Rect2 ArenaRect(float y, float height)
        => new(ChapterMap.ArenaGateX(ChapterId), y, 72f, height);

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
    private bool _guardianRestored;
    private bool _guardianAfterPlayed;
    private bool _completed;
    private bool _smokeMode;
    private float _time;

    private Node2D _guide = null!;
    private Control _guidePrompt = null!;
    private Sprite2D _exit = null!;
    private StaticBody2D? _arenaGate;
    private readonly List<Node2D> _restoreNodes = new();
    private readonly List<Control> _restorePrompts = new();
    private readonly List<StaticBody2D> _shortcutGates = new();
    private readonly List<Sprite2D> _fragmentsInWorld = new();
    private readonly Dictionary<Sprite2D, Vector2> _fragmentOrigins = new();
    private readonly List<TextureRect> _veils = new();

    /// Damage veil colour at a fraction across the map. Continuous, so neighbouring
    /// panels meet without a visible step at the seam.
    private Color DamageAt(float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return ChapterId == 2
            ? new Color(0.12f, 0.30f, 0.48f, 0.35f).Lerp(new Color(0.10f, 0.24f, 0.42f, 0.26f), t)
            : new Color(0.16f, 0.08f, 0.08f, 0.43f).Lerp(new Color(0.13f, 0.06f, 0.06f, 0.32f), t);
    }
    private readonly List<Node2D> _residents = new();
    private readonly List<Control> _residentPrompts = new();
    private readonly List<string> _residentTimelines = new();
    private readonly List<string> _residentNames = new();
    private readonly bool[] _requiredResidentVisited = new bool[3];
    private int[] _requiredResidentIndices = System.Array.Empty<int>();

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
        // The third anchor/relay opens the physical route. Cleaning smaller pollution
        // objects still gates the guardian encounter, so crossing early cannot start
        // or damage the boss.
        _boss.CombatEnabled = false;
        _boss.AggressionEnabled = false;
        _settlement = GetNodeOrNull<SettlementPanel>("HUD/SettlementPanel");

        StretchAuthoredScene();
        BuildWorld();
        _dialogue = new DialogueRunner { Name = "DialogueRunner" };
        AddChild(_dialogue);

        var bus = Events.Instance;
        if (bus != null)
        {
            bus.BossDefeated += OnGuardianRestored;
            bus.BossHealthChanged += OnGuardianHealthChanged;
            bus.NextLevelElementUnlocked += OnNextLevelElementUnlocked;
        }

        AudioManager.Instance?.PlayMusic("level1_underwater_ambient");
        _player.SetSafePoint(_player.GlobalPosition);
        _player.BroadcastHealth();
        BroadcastObjective();
        Events.Instance?.EmitSignal(Events.SignalName.ZoneEntered, ChapterRuntime.Zones[0]);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            ChapterId == 2
                ? "CHAPTER 2  FROZEN TRENCH Find Lanternfish and press E"
                : "CHAPTER 3  THE OLD LIGHTHOUSE Find the Lost Shoal and press E");

        RestoreProgressIfSaved();
        _smokeMode = Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-complete") >= 0;
        if (_smokeMode)
            CallDeferred(nameof(RunSmokeComplete));
        else if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--preview-guardian") >= 0)
            CallDeferred(nameof(PreviewGuardian));
    }

    /// Move the nodes placed in the .tscn (player spawn, guardian, camera bounds)
    /// onto the long map. The scene files stay authored against ChapterMap.AuthoredWidth.
    private void StretchAuthoredScene()
    {
        _player.Position = SX(_player.Position);
        // The guardian owns the far right of the map, not a scaled authored spot.
        _boss.Position = ChapterMap.BossPoint(ChapterId, _boss.Position.Y);
        var camera = _player.GetNodeOrNull<Camera2D>("Camera2D");
        if (camera != null)
        {
            camera.LimitLeft = 0;
            camera.LimitRight = Mathf.RoundToInt(MapWidth);
        }
    }

    private void BuildWorld()
    {
        var map = GetNode<Node2D>("Map");
        BuildBackdrop(map);
        BuildBoundsAndMaze(map);
        BuildGuide();
        BuildResidents();
        SelectRequiredResidents();
        BuildRestoreNodes(map);
        BuildFragments(map);
        BuildExit(map);
        BossAura.Attach(this, _boss, Palette.ArenaTint(ChapterId));
        ObjectiveGuide.Attach(this, CurrentTarget, () => _dialogue != null && _dialogue.IsActive);
        OptionalInteractionGuide.Attach(this, () => _dialogue != null && _dialogue.IsActive);
    }

    private void BuildBackdrop(Node2D map)
    {
        // Each chapter owns three painted panels laid head to tail across the map.
        string[] sources = { AssetLoader.ChapterBackground(ChapterId, 0), AssetLoader.ChapterBackground(ChapterId, 1), AssetLoader.ChapterBackground(ChapterId, 2) };
        for (int i = 0; i < 3; i++)
        {
            float x0 = ChapterMap.PanelStart(ChapterId, i);
            float panelWidth = ChapterMap.PanelWidth(ChapterId, i);
            var texture = AssetLoader.Texture(sources[i]);
            if (texture != null)
            {
                var art = new TextureRect
                {
                    Name = $"ChapterArt{i + 1}", Position = new Vector2(x0, 0), Size = new Vector2(panelWidth, MapHeight),
                    Texture = texture, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.Scale, ZIndex = -100,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    Modulate = Colors.White,
                };
                map.AddChild(art);
            }

            // Sampled from one continuous curve rather than a per-panel alpha step, so
            // the veil does not draw a band exactly on each backdrop seam.
            float t0 = x0 / MapWidth, t1 = (x0 + panelWidth) / MapWidth;
            var veil = new TextureRect
            {
                Name = $"DamageVeil{i + 1}", Position = new Vector2(x0, 0), Size = new Vector2(panelWidth, MapHeight),
                Texture = PlaceholderArt.HorizontalGradient(DamageAt(t0), DamageAt(t1)),
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ZIndex = -96, MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            map.AddChild(veil);
            _veils.Add(veil);

        }
        MapSeamBlender.Add(map, ChapterMap.PanelStart(ChapterId, 1), MapHeight, -94, Palette.ArenaTint(ChapterId), ChapterId * 100 + 1);
        MapSeamBlender.Add(map, ChapterMap.PanelStart(ChapterId, 2), MapHeight, -94, Palette.ArenaTint(ChapterId), ChapterId * 100 + 2);
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
            if (ChapterRuntime.ReefMazeEnabled)
            {
                AddWall(walls, rect);
                AddReefVisual(map, rect, seed++);
            }
            else seed++;
        }

        // The final arena has one readable entrance; the elemental collection opens it.
        AddWall(walls, ArenaRect(24, 320));
        AddWall(walls, ArenaRect(736, 320));
        AddReefVisual(map, ArenaRect(24, 320), seed++);
        AddReefVisual(map, ArenaRect(736, 320), seed++);
        _arenaGate = MakeGate(map, ArenaRect(344, 392), "GuardianGate", ChapterId == 2 ? Palette.ElementIce : Palette.ElementElectric);
    }

    private void BuildGuide()
    {
        bool ice = ChapterId == 2;
        _guide = new Node2D { Name = ice ? "LanternNPC" : "ShoalNPC", Position = SX(ice ? new Vector2(330, 530) : new Vector2(340, 650)) };
        AddChild(_guide);
        var sprite = new Sprite2D { Texture = AssetLoader.Texture(AssetLoader.NpcPortrait(ice ? "lantern" : "shoal")), ZIndex = 1 };
        if (sprite.Texture != null) PlaceholderArt.FitSprite(sprite, 152f);
        _guide.AddChild(sprite);
        InteractionSpacing.AddSolid(_guide, 152f);
        _guidePrompt = UiTheme.WorldPlate(_guide, 88f, ice ? "Lanternfish" : "Lost Shoal",
            ice ? "Flow guide" : "Power guide", "Talk");
        _guidePrompt.Visible = false;
        StartBob(_guide, 8f, 1.7f);
    }

    /// The chapter's painted residents. They are spoken to, never fought: the one
    /// creature this chapter fights is the guardian at the end.
    private void BuildResidents()
    {
        foreach (var member in ChapterCast.For(ChapterId))
        {
            var position = SX(member.Position);
            if (IsBossAreaOrAfter(position)) continue;

            var node = new Node2D { Name = $"Cast_{member.Id}", Position = position };
            node.AddToGroup("npc");
            AddChild(node);

            var portrait = AssetLoader.Texture(AssetLoader.NpcPortrait(member.PortraitId));
            var sprite = new Sprite2D { Texture = portrait ?? PlaceholderArt.RoundBlob(72, member.Tint), ZIndex = 1 };
            if (portrait != null) PlaceholderArt.FitSprite(sprite, member.DisplaySize);
            node.AddChild(sprite);
            InteractionSpacing.AddSolid(node, member.DisplaySize);
            NpcAttentionCue.Attach(node, sprite, member.Tint, member.DisplaySize);

            float labelY = member.DisplaySize * 0.56f + 16f;
            var prompt = UiTheme.WorldPlate(node, labelY, member.DisplayName, null, "Talk");
            prompt.Visible = false;

            StartBob(node, 6f, 2.1f + _residents.Count * 0.13f);
            _residents.Add(node);
            _residentPrompts.Add(prompt);
            _residentTimelines.Add(member.Id);
            _residentNames.Add(member.DisplayName);
        }
    }

    private void BuildRestoreNodes(Node2D map)
    {
        Vector2[] positions = ChapterId == 2
            ? new[] { SX(new Vector2(900, 220)), SX(new Vector2(1680, 880)), SX(new Vector2(2420, 230)) }
            : new[] { SX(new Vector2(980, 850)), SX(new Vector2(1850, 250)), SX(new Vector2(2820, 820)) };
        for (int i = 0; i < positions.Length; i++)
        {
            var node = new Node2D { Name = ChapterId == 2 ? $"FlowSwitch{i + 1}" : $"PowerRelay{i + 1}", Position = positions[i] };
            map.AddChild(node);
            var icon = new Sprite2D
            {
                Texture = AssetLoader.Texture(AssetLoader.MemoryIcon) ?? PlaceholderArt.RoundBlob(64, Palette.ForForm(RequiredForm)),
                Modulate = ChapterId == 2 ? new Color(0.65f, 0.88f, 1f, 0.62f) : new Color(0.44f, 0.82f, 0.68f, 0.62f),
            };
            PlaceholderArt.FitSprite(icon, 88f);
            node.AddChild(icon);
            InteractionSpacing.AddSolid(node, 88f, 46f);
            var prompt = UiTheme.WorldPlate(node, 72f, ChapterId == 2 ? "Flow Switch" : "Power Relay",
                $"{i + 1} of 3", "Restore");
            prompt.Visible = false;
            _restoreNodes.Add(node);
            _restorePrompts.Add(prompt);

            var gate = MakeGate(map,
                SR(i == 0 ? new Rect2(1130, 450, 70, 180) : i == 1 ? new Rect2(2390, 430, 70, 180) : new Rect2(2710, 430, 70, 180)),
                $"ShortcutGate{i + 1}", ChapterId == 2 ? Palette.ElementIce : Palette.PollutedTeal);
            _shortcutGates.Add(gate);
        }
    }

    private void BuildFragments(Node2D map)
    {
        Vector2[] ice = { new(210,210), new(600,870), new(740,230), new(1170,650), new(1370,480), new(1730,170), new(1960,900), new(2220,430), new(2500,900), new(2670,180), new(3010,900), new(3370,850) };
        Vector2[] electric = { new(190,540), new(520,190), new(740,520), new(1080,220), new(1280,850), new(1700,560), new(1940,900), new(2190,250), new(2470,560), new(2780,180), new(3150,860), new(3440,470) };
        foreach (var authored in ChapterId == 2 ? ice : electric)
        {
            var point = SX(authored);
            if (point.X >= _boss.GlobalPosition.X - 720f) continue;
            // Residents, the guide and the restore nodes are already in the tree, so a
            // pollution object that would sit on one is simply skipped.
            if (!ChapterMap.IsClearOfOccupants(this, point, FragmentClearance, "npc")) continue;
            if (_restoreNodes.Exists(n => n.GlobalPosition.DistanceTo(point) < FragmentClearance)) continue;
            var shard = new Sprite2D
            {
                Name = ChapterId == 2 ? "NutrientRunoff" : "OilPatch",
                Texture = AssetLoader.Texture(AssetLoader.ChapterElement(ChapterId))
                          ?? AssetLoader.Texture(AssetLoader.ShardIcon)
                          ?? PlaceholderArt.RoundBlob(56, Palette.ForForm(RequiredForm)),
                // The element art already carries its own colour, so it is left
                // unmodulated; a tint here only greys the painting out.
                Position = point, ZIndex = 3,
            };
            PlaceholderArt.FitSprite(shard, FragmentDisplaySize);
            // Same group chapter one uses, so tooling and audits see one kind of pickup.
            shard.AddToGroup("element");
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
            Position = ChapterMap.ExitPoint(ChapterId), Modulate = new Color(0.7f, 0.9f, 1f, 0.2f), ZIndex = -4,
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
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                ChapterId == 2
                    ? "The Chemical Waste Monster is close. Keep distance; press 2 only after it hits."
                    : "The Oil Monster is close. Keep distance; press 3 only after it hits.");
        }
        if (_stage == ObjectiveStage.ExitLevel && _player.GlobalPosition.DistanceTo(_exit.GlobalPosition) <= GameConstants.ExitReachDistance + 30f)
            CompleteChapter();
    }

    private void UpdateZone()
    {
        int next = ChapterMap.ZoneAt(ChapterId, _player.GlobalPosition.X);
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
        for (int i = 0; i < _residents.Count; i++)
            _residentPrompts[i].Visible = dialogueFree && !IsBossAreaOrAfter(_residents[i].GlobalPosition) && Near(_residents[i]);
    }

    private void UpdateFragments()
    {
        for (int i = _fragmentsInWorld.Count - 1; i >= 0; i--)
        {
            var shard = _fragmentsInWorld[i];
            if (!IsInstanceValid(shard)) { _fragmentsInWorld.RemoveAt(i); continue; }
            var origin = _fragmentOrigins[shard];
            shard.Position = origin + new Vector2(0, Mathf.Sin(_time * 2.2f + i * 0.7f) * 9f);
            shard.Visible = _stage == ObjectiveStage.CollectShards;
            shard.Modulate = new Color(shard.Modulate.R, shard.Modulate.G, shard.Modulate.B, 1f);
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
            // Consume it: this node runs before the PauseInput autoload, which would
            // otherwise see the same event and immediately clear the pause again.
            GetViewport().SetInputAsHandled();
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
            InteractionSpacing.FrameConversation(_player, _guide, InteractionSpacing.NpcConversationDistance);
            _dialogue.Play(GuideTimeline, () => Advance(ObjectiveStage.TalkStarfish));
            GetViewport().SetInputAsHandled();
            return;
        }
        if (TryRequiredResidentInteraction())
        {
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
        // Residents are optional colour, so they are offered only once nothing
        // required is in reach.
        for (int i = 0; i < _residents.Count; i++)
        {
            if (IsBossAreaOrAfter(_residents[i].GlobalPosition)) continue;
            if (Near(_residents[i]))
            {
                InteractionSpacing.FrameConversation(_player, _residents[i], InteractionSpacing.NpcConversationDistance);
                _dialogue.Play(_residentTimelines[i]);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
    }

    private bool IsBossAreaOrAfter(Vector2 worldPosition)
        => worldPosition.X >= ChapterMap.ArenaGateX(ChapterId) - 1f;

    /// What the guide arrow points at for the current step.
    private Node2D? CurrentTarget() => _stage switch
    {
        ObjectiveStage.FindNpc => _guide,
        ObjectiveStage.TalkStarfish or ObjectiveStage.TalkSeaweed
            => RequiredResidentForNode(_restoredNodes) ??
               (_restoredNodes < _restoreNodes.Count ? _restoreNodes[_restoredNodes] : null),
        ObjectiveStage.CollectShards => Nearest(_fragmentsInWorld),
        ObjectiveStage.DefeatBoss => _boss.NextElementDrop ?? _boss,
        ObjectiveStage.ExitLevel => _exit,
        _ => null,
    };

    private Node2D? Nearest<T>(System.Collections.Generic.IEnumerable<T> nodes) where T : Node2D
    {
        Node2D? best = null;
        float bestDistance = float.MaxValue;
        foreach (var node in nodes)
        {
            if (!IsInstanceValid(node) || !node.Visible) continue;
            float d = node.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
            if (d < bestDistance) { bestDistance = d; best = node; }
        }
        return best;
    }

    private bool IsNodeAvailable(int index) => index == _restoredNodes &&
        RequiredResidentForNode(index) == null &&
        ((index == 0 && _stage == ObjectiveStage.TalkStarfish) ||
         (index > 0 && _stage == ObjectiveStage.TalkSeaweed));

    private void SelectRequiredResidents()
    {
        _requiredResidentIndices = ChapterId == 2
            ? new[] { 0, 2, 5 }  // source → toxic cloud → unsafe water
            : new[] { 0, 2, 5 }; // oil → bleaching → spread
    }

    private Node2D? RequiredResidentForNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= _requiredResidentVisited.Length) return null;
        if (_requiredResidentVisited[nodeIndex]) return null;
        if (nodeIndex >= _requiredResidentIndices.Length) return null;
        int residentIndex = _requiredResidentIndices[nodeIndex];
        if (residentIndex < 0 || residentIndex >= _residents.Count) return null;
        var resident = _residents[residentIndex];
        if (!IsInstanceValid(resident) || IsBossAreaOrAfter(resident.GlobalPosition)) return null;
        return resident;
    }

    private bool TryRequiredResidentInteraction()
    {
        if (_stage != ObjectiveStage.TalkStarfish && _stage != ObjectiveStage.TalkSeaweed) return false;
        int nodeIndex = _restoredNodes;
        var resident = RequiredResidentForNode(nodeIndex);
        if (resident == null || !Near(resident)) return false;

        int residentIndex = _residents.IndexOf(resident);
        if (residentIndex < 0 || residentIndex >= _residentTimelines.Count) return false;

        InteractionSpacing.FrameConversation(_player, resident, InteractionSpacing.NpcConversationDistance);
        _requiredResidentVisited[nodeIndex] = true;
        string nextDevice = ChapterId == 2 ? "Flow Switch" : "Power Relay";
        _dialogue.Play(_residentTimelines[residentIndex], () =>
        {
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                $"Now restore {nextDevice} {nodeIndex + 1}.");
            RefreshObjective();
        });
        return true;
    }

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
        string[] chapterFacts = ChapterId == 2
            ? new[]
            {
                "Chemical wastewater can travel from land into the sea through drains and pipes.",
                "Toxic waste can spread quietly through water and mud, even when the surface looks calm.",
                "Moving clean water through the trench gives eggs and seaweed a safer place to recover.",
            }
            : new[]
            {
                "The ocean absorbs most of the extra heat trapped by greenhouse gases.",
                "Long heat stress can make coral bleach and lose its main source of food.",
                "Cleaner energy and lower emissions reduce heat pressure on reefs.",
            };
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint, chapterFacts[index]);
        AudioManager.Instance?.PlaySfx("objective_advance");
        if (index == 0) Advance(ObjectiveStage.TalkSeaweed);
        else if (_restoredNodes < 3) RefreshObjective();
        else if (_restoredNodes >= 3)
        {
            OpenArenaGate();
            Advance(ObjectiveStage.CollectShards);
        }
    }

    private void SetZoneRestored(int index)
    {
        if (index < _veils.Count)
            _veils[index].CreateTween().TweenProperty(_veils[index], "modulate:a", 0.18f, 0.85f);
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
        if (_fragments == 1 || _fragments >= RequiredFragments)
        {
            string message = ChapterId == 2
                ? (_fragments >= RequiredFragments
                    ? "Enough chemical waste is cleaned. Shimmer can face the Chemical Waste Monster now."
                    : "Chemical waste cleaned. Shimmer's cleanup power is building.")
                : (_fragments >= RequiredFragments
                    ? "Enough oil is cleaned. Shimmer can face the Oil Monster now."
                    : "Oil patch cleaned. Shimmer's cleanup power is building.");
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, message);
        }
        AudioManager.Instance?.PlaySfx("pickup_water");
        shard.CreateTween().TweenProperty(shard, "scale", shard.Scale * 1.6f, 0.18f)
            .Finished += shard.QueueFree;
        if (_fragments >= RequiredFragments)
        {
            ClearRemainingFragments();
            _boss.CombatEnabled = true;
            Advance(ObjectiveStage.DefeatBoss);
            Events.Instance?.EmitSignal(Events.SignalName.BossHealthChanged, _boss.CurrentHealth, _boss.MaxHealthValue);
        }
    }

    private void ClearRemainingFragments()
    {
        foreach (var shard in _fragmentsInWorld)
            if (IsInstanceValid(shard))
                shard.QueueFree();
        _fragmentsInWorld.Clear();
        _fragmentOrigins.Clear();
    }

    /// The three restoration nodes power this doorway, so the doorway opens with
    /// the third node instead of waiting for the later shard objective. Disable the
    /// shape as well as its layer before fading it out; this avoids an invisible
    /// collision body during the tween or on a slow physics frame.
    private void OpenArenaGate()
    {
        if (_arenaGate == null || !IsInstanceValid(_arenaGate)) return;

        var gate = _arenaGate;
        _arenaGate = null;
        gate.CollisionLayer = 0;
        gate.CollisionMask = 0;
        foreach (var child in gate.GetChildren())
            if (child is CollisionShape2D shape)
                shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        gate.CreateTween().TweenProperty(gate, "modulate:a", 0f, 0.65f)
            .Finished += gate.QueueFree;
        _boss.AggressionEnabled = true;
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            ChapterId == 2
                ? "The three Flow Switches have opened the nursery gate."
                : "The three Power Relays have opened the lighthouse gate.");
    }

    private void OnGuardianHealthChanged(int current, int max)
    {
        if (_stage == ObjectiveStage.DefeatBoss && current > 0 && current <= max / 2)
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                "The shell is loosening. Keep your distance and use the cleanup power you earned." );
    }

    private void OnGuardianRestored()
    {
        if (_stage != ObjectiveStage.DefeatBoss) return;
        _guardianRestored = true;
        for (int i = 0; i < 3; i++) SetZoneRestored(i);
        _exit.CreateTween().TweenProperty(_exit, "modulate", Colors.White, 0.9f);
        if (!_guardianAfterPlayed)
        {
            _guardianAfterPlayed = true;
            InteractionSpacing.FrameConversation(_player, _boss, InteractionSpacing.GuardianConversationDistance);
            _dialogue.Play(GuardianTimeline, () => _dialogue.Play(EndingTimeline, () => Advance(ObjectiveStage.ExitLevel)));
        }
        else
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                ChapterId == 2
                    ? "The Chemical Waste Monster is contained. Follow the clean current toward the lighthouse."
                    : "The Oil Monster is gone. Light the Silent Lighthouse.");
    }

    private void OnNextLevelElementUnlocked(int form)
    {
        if (ChapterId != 2 || !_guardianRestored || _stage != ObjectiveStage.DefeatBoss) return;
        if ((ElementForm)form != ElementForm.Electric) return;
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
            ? "The three Flow Switches are moving cleaner water through the nursery again. The chemical waste has been contained.\nPollution begins on land. Restoring flow gives this habitat time to heal."
            : "The safe circuit is running, and the oil monster has been pulled away from the reef. Coral recovery will take time.\nProtecting the ocean means stopping pollution at its source, not only cleaning it up later." );
    }

    private void Advance(ObjectiveStage next)
    {
        _stage = next;
        UpdateObjectiveOverride();
        Events.Instance?.EmitSignal(Events.SignalName.ObjectiveAdvanced, (int)next);
        if (next == ObjectiveStage.CollectShards)
            Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, _fragments, RequiredFragments);
        if (next == ObjectiveStage.Complete)
            Events.Instance?.EmitSignal(Events.SignalName.ObjectiveCompleted);
        else AudioManager.Instance?.PlaySfx("objective_advance");
    }

    private void BroadcastObjective()
    {
        UpdateObjectiveOverride();
        Events.Instance?.EmitSignal(Events.SignalName.ObjectiveAdvanced, (int)_stage);
        Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, _fragments, RequiredFragments);
    }

    private void RefreshObjective()
    {
        UpdateObjectiveOverride();
        Events.Instance?.EmitSignal(Events.SignalName.ObjectiveAdvanced, (int)_stage);
    }

    private void UpdateObjectiveOverride()
    {
        string? text = null;
        string device = ChapterId == 2 ? "Flow Switch" : "Power Relay";
        string pollutant = ChapterId == 2 ? "chemical waste" : "oil patches";
        string boss = ChapterId == 2 ? "Chemical Waste Monster" : "Oil Monster";

        if (_stage == ObjectiveStage.TalkStarfish || _stage == ObjectiveStage.TalkSeaweed)
        {
            var resident = RequiredResidentForNode(_restoredNodes);
            if (resident != null)
            {
                int index = _residents.IndexOf(resident);
                string name = index >= 0 && index < _residentNames.Count ? _residentNames[index] : "the next resident";
                text = $"Talk to {name}, then restore {device} {_restoredNodes + 1}";
            }
            else if (_restoredNodes < _restoreNodes.Count)
            {
                text = $"Restore {device} {_restoredNodes + 1}";
            }
        }
        else if (_stage == ObjectiveStage.CollectShards)
        {
            text = $"Clean {pollutant} so Shimmer can face the {boss}";
        }
        else if (_stage == ObjectiveStage.DefeatBoss)
        {
            int key = ChapterId == 2 ? 2 : 3;
            text = $"Keep distance. Press {key} to use cleanup power on the {boss}";
        }

        ChapterRuntime.SetObjectiveOverride(text);
    }

    private bool Near(Node2D node) => _player.GlobalPosition.DistanceTo(node.GlobalPosition) <= InteractionRange;

    private void PersistProgress()
    {
        const int boundaryLevel = 3;
        var boundaryStage = ChapterId == 2 ? ObjectiveStage.FindNpc : ObjectiveStage.Complete;
        var state = new SaveState(SaveManager.SaveVersion, Time.GetDatetimeStringFromSystem(true), _player.CurrentForm,
            _player.CurrentHealth, 0, 0, 0, boundaryStage,
            ChapterId == 2 ? ElementForm.Electric : null, true, true, boundaryLevel, 0);
        SaveManager.Instance?.SaveState(state);
    }

    private void RestoreProgressIfSaved()
    {
        var state = SaveManager.Instance?.LoadState();
        if (state == null || state.CurrentLevel != ChapterId || state.ObjectiveStage == ObjectiveStage.Complete) return;
        // Chapter interaction topology is short; resume safely at the start rather
        // than restoring half-removed runtime gates without their authored visuals.
        _player.RestoreState(state.CurrentHealth, RequiredForm);
        _skills.RestoreCharges(0, 0, 0);
        Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, _fragments, RequiredFragments);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            "Progress restored. This chapter's restoration route begins at the entrance." );
    }

    private void RunSmokeComplete()
    {
        Advance(ObjectiveStage.TalkStarfish);
        RestoreNode(0); RestoreNode(1); RestoreNode(2);
        if (_arenaGate != null)
        {
            GD.PushError($"CHAPTER_GATE_FAILED level={ChapterId}: arena gate stayed active after node 3");
            GetTree().Quit(1);
            return;
        }
        GD.Print($"CHAPTER_GATE_OK level={ChapterId}: arena gate opened after node 3");
        var copy = _fragmentsInWorld.ToArray();
        for (int i = 0; i < RequiredFragments && i < copy.Length; i++) CollectFragment(copy[i]);
        _guardianBeatPlayed = true;
        _boss.ApplyDamage(_boss.MaxHealthValue);
        if (ChapterId == 2)
        {
            if (_boss.NextElementDrop == null)
            {
                GD.PushError("CHAPTER_KEY_FAILED level=2: Electric current did not spawn");
                GetTree().Quit(1);
                return;
            }
            GD.Print("CHAPTER_KEY_OK level=2: Electric current spawned");
            _boss.CollectNextElementDrop();
        }
        else if (_boss.NextElementDrop != null)
        {
            GD.PushError("CHAPTER_KEY_FAILED level=3: final chapter spawned a nonexistent next key");
            GetTree().Quit(1);
            return;
        }
        else GD.Print("CHAPTER_KEY_OK level=3: final chapter correctly has no next key");
        Advance(ObjectiveStage.ExitLevel);
        CompleteChapter();
        GD.Print($"CHAPTER_SMOKE_OK level={ChapterId} stage={_stage} fragments={_fragments} nodes={_restoredNodes}");
    }

    private void PreviewGuardian()
    {
        _guardianBeatPlayed = true;
        _fragments = RequiredFragments;
        for (int i = 0; i < 3; i++) SetZoneRestored(i);
        OpenArenaGate();
        _boss.CombatEnabled = true;
        _player.GlobalPosition = _boss.GlobalPosition + new Vector2(-300f, 0f);
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
        // Skinny vertical blockers read as stray rectangular pillars in the painted
        // world, especially when the camera frames them behind HUD/location plates.
        // Keep their collision in AddWall/MakeGate, but do not draw a separate
        // decorative tile over the background.
        if (rect.Size.Y > rect.Size.X * 1.65f)
            return;

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
            Color = ChapterId == 2 ? new Color(0.48f, 0.64f, 0.72f, 0.52f) : new Color(0.34f, 0.48f, 0.45f, 0.50f),
            ZIndex = -92,
        };
        map.AddChild(poly);
    }

    private StaticBody2D MakeGate(Node2D map, Rect2 rect, string name, Color color)
    {
        var gate = new StaticBody2D { Name = name, Position = rect.GetCenter(), CollisionLayer = 32, ZIndex = -30 };
        gate.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = rect.Size } });
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

    private static void StartBob(Node2D node, float amount, float seconds)
    {
        Vector2 rest = node.Position;
        var tween = node.CreateTween().SetLoops();
        tween.TweenProperty(node, "position", rest + new Vector2(0, -amount), seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(node, "position", rest + new Vector2(0, amount), seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}

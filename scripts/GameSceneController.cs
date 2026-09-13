using Godot;

namespace ShallowSeaDream;

/// res://scripts/GameSceneController.cs
/// Level 1 root coordinator (story-first overhaul). Owns the subsystem nodes and the
/// level lifecycle across THREE connected sub-zones of the 潮湾苗圃 nursery
/// (浅滩 / 沉积带 / 巢母深处, item 7): map/wall layout, NPC + new-NPC wiring with
/// multi-stage 岚婆婆, examinable 残片笔记 lore objects (item 5), the 微光潮汐记忆 shard
/// vignettes (item 3), the narrative boss purification sequence (item 4/6), the
/// opening + ending beats (item 6), zone-entry beats, pause/restart, exit-reach, and
/// save/restore. The whole level is always visible (fog removed, item 1).
public partial class GameSceneController : Node2D
{
    [Export] public string TitleScenePath = "res://scenes/title.tscn";

    // Playfield is widened to 3 zones laid out left→right (item 7).
    /// Chapter one's length comes from its three paintings; hand-authored X
    /// coordinates below are still written against ChapterMap.AuthoredWidth and are
    /// restretched through SX / SR.
    private static float MapWidth => ChapterMap.TotalWidth(1);
    private const float MapHeight = 1080f;
    // Zone boundaries follow the painted panel joins instead of fixed offsets.
    private static float ZoneSedimentX => ChapterMap.PanelStart(1, 1);  // 浅滩 → 沉积带
    private static float ZoneDepthsX => ChapterMap.PanelStart(1, 2);    // 沉积带 → 巢母深处

    private static Vector2 SX(Vector2 authored) => ChapterMap.Place(1, authored);
    private static Rect2 SR(Rect2 authored) => ChapterMap.Place(1, authored);

    /// GameConstants.ExitPoint was authored against the old 3600-wide map; on the long
    /// map the gate belongs at the right edge, past the guardian.
    private static Vector2 ChapterExitPoint => ChapterMap.ExitPoint(1);

    // Fixed draw order below entities (player/NPCs/boss default to ZIndex 0).
    private const int BackgroundZ = -100; // zone art, furthest back
    private const int WallZ = -50;        // wall visuals, above art but below entities

    private bool _paused;
    private bool _exitReached;
    private bool _bossDefeated;
    private ElementForm _unlockedNext = ElementForm.Base;
    private string _currentZone = "";
    private bool _bossPrePlayed;
    private bool _bossMidPlayed;
    private bool _grannyMidPlayed;
    private readonly System.Collections.Generic.List<TextureRect> _pollutionVeils = new();

    /// Pollution veil colour at a fraction across the map, 0 at the mouth to 1 at the
    /// guardian. Continuous by construction, so adjacent zones meet without a step.
    private static Color PollutionAt(float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return new Color(0.03f, 0.18f, 0.20f, 0.16f).Lerp(new Color(0.01f, 0.06f, 0.09f, 0.34f), t);
    }
    private readonly System.Collections.Generic.List<System.Collections.Generic.List<Sprite2D>> _zoneInvaders = new();
    private readonly System.Collections.Generic.Dictionary<int, Texture2D> _keyedInvaders = new();
    private readonly bool[] _zoneRestored = new bool[3];
    private readonly System.Collections.Generic.List<System.Collections.Generic.List<StaticBody2D>> _zoneShortcutGates =
        new() { new(), new(), new() };

    private Player? _player;
    private ObjectiveManager? _objectives;
    private SkillSystem? _skills;
    private DialogueRunner _dialogue = null!;
    private Sprite2D? _exitMarker;

    // Interactables (NPCs + lore objects), examined with E within range.
    private sealed class Interactable
    {
        public Node2D Node = null!;
        public string Timeline = "";          // for NPCs (first talk)
        public string? FollowUp;               // optional repeat-talk timeline (item 6)
        public LoreNote? Note;                 // for lore objects
        public Control? Prompt;                // shown only inside interaction range
        public Control? Identity;              // persistent NPC nameplate
        public System.Action? OnFirstDone;
        public bool Talked;
    }
    private readonly System.Collections.Generic.List<Interactable> _interactables = new();

    public override void _EnterTree() => ChapterRuntime.SetChapter(1);

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        _objectives = GetNodeOrNull<ObjectiveManager>("ObjectiveManager");
        _skills = GetNodeOrNull<SkillSystem>("SkillSystem");

        SetupMap();
        SetupNpcs();
        SetupLoreObjects();

        _dialogue = new DialogueRunner { Name = "DialogueRunner" };
        AddChild(_dialogue);

        AudioManager.Instance?.PlayMusic("level1_underwater_ambient");

        var bus = Events.Instance;
        if (bus != null)
        {
            bus.NextLevelElementUnlocked += OnNextLevelElementUnlocked;
            bus.ObjectiveCompleted += OnObjectiveCompleted;
            bus.BossDefeated += OnBossDefeated;
            bus.MemoryUnlocked += OnMemoryUnlocked;
            bus.BossHealthChanged += OnBossHealthChanged;
            bus.ShardProgressChanged += OnShardProgressChanged;
            bus.ObjectiveAdvanced += OnObjectiveAdvanced;
        }

        StretchAuthoredScene();
        if (_player != null) _player.SetSafePoint(_player.GlobalPosition);

        RestoreProgressIfSaved();
        _player?.BroadcastHealth();
        _objectives?.BroadcastInitial();

        // Opening beat (item 6) — deferred so the HUD/dialogue are ready.
        CallDeferred(nameof(PlayOpening));
    }

    private void PlayOpening()
    {
        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--skip-opening") >= 0) return;
        if (_objectives != null && _objectives.Stage == ObjectiveStage.FindNpc)
            _dialogue.Play(NarrativeData.Opening);
    }

    // --- Setup ---

    /// Move the nodes placed in game_scene1.tscn (player spawn, guardian, camera
    /// bounds) onto the long map. The scene file stays authored against
    /// ChapterMap.AuthoredWidth so it remains readable.
    private void StretchAuthoredScene()
    {
        if (_player != null)
        {
            _player.Position = SX(_player.Position);
            var camera = _player.GetNodeOrNull<Camera2D>("Camera2D");
            if (camera != null)
            {
                camera.LimitLeft = 0;
                camera.LimitRight = Mathf.RoundToInt(MapWidth);
            }
        }
        var boss = GetNodeOrNull<Node2D>("BroodMother");
        if (boss != null)
        {
            boss.Position = ChapterMap.BossPoint(1, boss.Position.Y);
            BossAura.Attach(this, boss, Palette.ArenaTint(1));
        }
        ObjectiveGuide.Attach(this, CurrentTarget, () => _dialogue != null && _dialogue.IsActive);
    }

    /// What the guide arrow points at for the current step.
    private Node2D? CurrentTarget()
    {
        switch (_objectives?.Stage)
        {
            case ObjectiveStage.FindNpc: return GetNodeOrNull<Node2D>("GrannyLan");
            case ObjectiveStage.TalkStarfish: return GetNodeOrNull<Node2D>("StarfishNPC");
            case ObjectiveStage.TalkSeaweed: return GetNodeOrNull<Node2D>("SeaweedNPC");
            case ObjectiveStage.DefeatBoss: return GetNodeOrNull<Node2D>("BroodMother");
            case ObjectiveStage.ExitLevel: return _exitMarker;
            case ObjectiveStage.CollectShards:
                Node2D? best = null;
                float bestDistance = float.MaxValue;
                foreach (var n in GetTree().GetNodesInGroup("element"))
                {
                    if (n is not Node2D shard || !shard.Visible || _player == null) continue;
                    float d = shard.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
                    if (d < bestDistance) { bestDistance = d; best = shard; }
                }
                return best;
            default: return null;
        }
    }

    private void SetupMap()
    {
        var map = GetNodeOrNull<Node2D>("Map");
        if (map == null) return;

        // Use the three existing hand-painted zone illustrations as the visual spine.
        // This keeps the world coherent with the title and makes pollution/restoration
        // readable in a portfolio recording instead of hiding the art behind gradients.
        BuildBackdrop(map);
        AddExitMarker(map);

        var walls = map.GetNodeOrNull<StaticBody2D>("Walls");
        if (walls != null)
        {
            // Outer bounds for the widened 3-zone field.
            AddWall(walls, new Rect2(0, 0, MapWidth, 24));
            AddWall(walls, new Rect2(0, MapHeight - 24, MapWidth, 24));
            AddWall(walls, new Rect2(0, 0, 24, MapHeight));
            AddWall(walls, new Rect2(MapWidth - 24, 0, 24, MapHeight));
            BuildOrganicMaze(map, walls);
        }
    }

    /// Restore the original game's maze identity inside the newer three-zone arc.
    /// Every zone has a readable route, optional side pockets and one pollution gate
    /// that dissolves when that part of the sea is restored. Collision rectangles are
    /// dressed with irregular, textured reef silhouettes so they belong to the art.
    private void BuildOrganicMaze(Node2D map, StaticBody2D walls)
    {
        var zoneLayouts = new[]
        {
            new[]
            {
                new Rect2(360, 80, 100, 300), new Rect2(360, 620, 100, 360),
                new Rect2(650, 480, 300, 80), new Rect2(1120, 100, 80, 350),
                new Rect2(980, 650, 300, 80),
            },
            new[]
            {
                new Rect2(1420, 80, 90, 360), new Rect2(1300, 560, 380, 90),
                new Rect2(1820, 340, 90, 500), new Rect2(2000, 180, 360, 90),
                new Rect2(2020, 680, 360, 90), new Rect2(2320, 300, 80, 350),
            },
            new[]
            {
                new Rect2(2500, 260, 380, 90), new Rect2(2720, 520, 90, 420),
                new Rect2(3000, 80, 90, 320), new Rect2(2920, 700, 420, 90),
                new Rect2(3280, 160, 90, 300), new Rect2(3370, 760, 200, 90),
            },
        };

        int seed = 31;
        if (ChapterRuntime.ReefMazeEnabled)
        {
            for (int zone = 0; zone < zoneLayouts.Length; zone++)
            foreach (var rect in zoneLayouts[zone])
                AddReefObstacle(map, walls, rect, zone, seed++);
        }
        else
        {
            // Keep the seed advancing so the decorative variation downstream is
            // identical whether or not the maze is drawn.
            foreach (var zoneRects in zoneLayouts) seed += zoneRects.Length;
        }

        // Optional shortcuts. Each has a longer open route around it, so progress can
        // never deadlock; restoration simply makes later traversal more graceful.
        AddPollutionGate(map, SR(new Rect2(760, 730, 80, 180)), 0, seed++);
        AddPollutionGate(map, SR(new Rect2(1680, 470, 140, 70)), 1, seed++);
        AddPollutionGate(map, SR(new Rect2(3180, 480, 100, 180)), 2, seed++);
    }

    private static Vector2[] OrganicOutline(Vector2 size, int seed)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)seed * 7919UL };
        const int count = 24;
        var points = new Vector2[count];
        float hx = size.X * 0.57f, hy = size.Y * 0.57f;
        // A noisy superellipse keeps enough coverage for the rectangular collision,
        // while rounded ends and uneven shoulders read as a natural reef formation.
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            float x = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.46f) * hx;
            float y = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.46f) * hy;
            float noise = rng.RandfRange(0.88f, 1.14f);
            points[i] = new Vector2(x * noise, y * noise);
        }
        return points;
    }

    private static Color ReefTint(int zone) => zone switch
    {
        0 => new Color(0.38f, 0.66f, 0.64f, 0.94f),
        1 => new Color(0.26f, 0.48f, 0.51f, 0.96f),
        _ => new Color(0.18f, 0.35f, 0.40f, 0.98f),
    };

    private static void AddReefVisual(Node2D parent, Vector2 center, Vector2 size,
        int zone, int seed, bool polluted)
    {
        bool horizontal = size.X > size.Y * 1.45f;
        bool vertical = size.Y > size.X * 1.45f;
        float ratio = Mathf.Max(size.X, size.Y) / Mathf.Max(1f, Mathf.Min(size.X, size.Y));
        int pieces = (horizontal || vertical) ? Mathf.Clamp(Mathf.CeilToInt(ratio), 2, 5) : 1;
        var rng = new RandomNumberGenerator { Seed = (ulong)(seed + 113) * 6151UL };

        for (int piece = 0; piece < pieces; piece++)
        {
            float along = (piece + 0.5f) / pieces - 0.5f;
            Vector2 pieceCenter;
            Vector2 pieceSize;
            if (horizontal)
            {
                float step = size.X / pieces;
                pieceCenter = center + new Vector2(along * size.X,
                    rng.RandfRange(-size.Y * 0.10f, size.Y * 0.10f));
                pieceSize = new Vector2(step * 1.48f, size.Y * rng.RandfRange(1.25f, 1.50f));
            }
            else if (vertical)
            {
                float step = size.Y / pieces;
                pieceCenter = center + new Vector2(
                    rng.RandfRange(-size.X * 0.10f, size.X * 0.10f), along * size.Y);
                pieceSize = new Vector2(size.X * rng.RandfRange(1.25f, 1.50f), step * 1.48f);
            }
            else
            {
                pieceCenter = center;
                pieceSize = size * 1.18f;
            }

            int pieceSeed = seed * 11 + piece;
            var outline = OrganicOutline(pieceSize, pieceSeed);
            var uv = new Vector2[outline.Length];
            // World-locked UVs keep the source painting continuous across the
            // overlapping blobs; they merge into one reef instead of a tiled bar.
            var uvOffset = new Vector2(150 + (seed * 137) % 620, 120 + (seed * 83) % 680);
            for (int i = 0; i < outline.Length; i++) uv[i] = outline[i] + pieceCenter + uvOffset;
            var fill = new Polygon2D
            {
                Name = polluted ? "PollutionNetVisual" : "ReefVisual",
                Position = pieceCenter,
                Polygon = outline,
                UV = uv,
                Texture = AssetLoader.Texture(AssetLoader.WallTile),
                TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                Color = polluted ? new Color(0.18f, 0.48f, 0.44f, 0.96f) : ReefTint(zone),
                ZIndex = WallZ,
            };
            parent.AddChild(fill);

            if (!polluted && pieces > 1) continue;

            var edgePoints = new Vector2[outline.Length + 1];
            System.Array.Copy(outline, edgePoints, outline.Length);
            edgePoints[^1] = outline[0];
            var edge = new Line2D
            {
                Name = "ReefEdge",
                Position = pieceCenter,
                Points = edgePoints,
                Width = polluted ? 4f : 2.5f,
                DefaultColor = polluted
                    ? new Color(Palette.PollutedTealBright.R, Palette.PollutedTealBright.G, Palette.PollutedTealBright.B, 0.76f)
                    : new Color(Palette.CoastalCyan.R, Palette.CoastalCyan.G, Palette.CoastalCyan.B, zone == 0 ? 0.34f : 0.20f),
                Antialiased = true,
                ZIndex = WallZ + 1,
            };
            parent.AddChild(edge);
        }
    }

    private static void AddReefObstacle(Node2D map, StaticBody2D walls, Rect2 rect, int zone, int seed)
    {
        AddWall(walls, rect);
        AddReefVisual(map, rect.GetCenter(), rect.Size, zone, seed, polluted: false);
    }

    private void AddPollutionGate(Node2D map, Rect2 rect, int zone, int seed)
    {
        var gate = new StaticBody2D
        {
            Name = $"PollutionShortcut_{zone + 1}",
            Position = rect.GetCenter(),
            CollisionLayer = 32,
        };
        gate.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = rect.Size },
        });
        AddReefVisual(gate, Vector2.Zero, rect.Size, zone, seed, polluted: true);
        map.AddChild(gate);
        _zoneShortcutGates[zone].Add(gate);
    }

    private void AddExitMarker(Node2D map)
    {
        var texture = AssetLoader.Texture(AssetLoader.MemoryIcon);
        _exitMarker = new Sprite2D
        {
            Name = "TidalGate",
            Texture = texture ?? PlaceholderArt.RoundBlob(72, Palette.CoastalCyan),
            Position = ChapterExitPoint,
            ZIndex = -5,
            Modulate = new Color(0.55f, 0.78f, 0.84f, 0.34f),
        };
        PlaceholderArt.FitSprite(_exitMarker, 92f);
        map.AddChild(_exitMarker);
        var tween = _exitMarker.CreateTween().SetLoops();
        tween.TweenProperty(_exitMarker, "scale", _exitMarker.Scale * 1.08f, 1.25f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_exitMarker, "scale", _exitMarker.Scale, 1.25f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void BuildBackdrop(Node2D map)
    {
        AddZoneArt(map, 0, 0f, ZoneSedimentX, NarrativeData.ZoneShallows);
        AddZoneArt(map, 1, ZoneSedimentX, ZoneDepthsX, NarrativeData.ZoneSediment);
        AddZoneArt(map, 2, ZoneDepthsX, MapWidth, NarrativeData.ZoneDepths);
    }

    private void AddZoneArt(Node2D map, int zone, float x0, float x1, string zoneName)
    {
        var tex = AssetLoader.Texture(AssetLoader.ZoneBackground(zoneName));
        if (tex != null)
        {
            map.AddChild(new TextureRect
            {
                Name = $"ZoneArt{zone + 1}",
                Position = new Vector2(x0, 0),
                Size = new Vector2(x1 - x0, MapHeight),
                Texture = tex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ZIndex = BackgroundZ,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }

        // One continuous "further in, more polluted" curve sampled over this zone's
        // span. Flat per-zone colours stepped at the boundary, which lands on the
        // backdrop seam and read as a band painted across the art.
        var veil = new TextureRect
        {
            Name = $"PollutionVeil{zone + 1}",
            Position = new Vector2(x0, 0),
            Size = new Vector2(x1 - x0, MapHeight),
            Texture = PlaceholderArt.HorizontalGradient(PollutionAt(x0 / MapWidth), PollutionAt(x1 / MapWidth)),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            ZIndex = BackgroundZ + 2,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        map.AddChild(veil);
        _pollutionVeils.Add(veil);

        var invaders = new System.Collections.Generic.List<Sprite2D>();
        _zoneInvaders.Add(invaders);
        var placements = new[]
        {
            new Vector2(x0 + (x1 - x0) * 0.24f, 270),
            new Vector2(x0 + (x1 - x0) * 0.55f, 720),
            new Vector2(x0 + (x1 - x0) * 0.82f, 420),
        };
        for (int i = 0; i < placements.Length; i++)
        {
            var invaderTex = AssetLoader.Texture(AssetLoader.Invader(i + 1));
            if (invaderTex == null) continue;
            if (!_keyedInvaders.TryGetValue(i, out var keyedInvader))
            {
                keyedInvader = PlaceholderArt.KeyPresentationTexture(invaderTex, 0.035f);
                _keyedInvaders[i] = keyedInvader;
            }
            var sprite = new Sprite2D
            {
                Name = $"PollutionInvader_{zone}_{i}",
                Texture = keyedInvader,
                Position = placements[i],
                ZIndex = BackgroundZ + 8,
                Modulate = new Color(0.68f, 0.84f, 0.84f, 0.42f),
            };
            PlaceholderArt.FitSprite(sprite, 110f + i * 14f);
            map.AddChild(sprite);
            invaders.Add(sprite);
            float baseY = sprite.Position.Y;
            var tween = sprite.CreateTween().SetLoops();
            tween.TweenProperty(sprite, "position:y", baseY - 16f, 1.8f + i * 0.25f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(sprite, "position:y", baseY + 16f, 1.8f + i * 0.25f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
    }

    private static void AddWall(StaticBody2D walls, Rect2 rect)
    {
        var shape = new CollisionShape2D
        {
            Position = rect.Position + rect.Size * 0.5f,
            Shape = new RectangleShape2D { Size = rect.Size },
        };
        walls.AddChild(shape);
    }

    private void SetupNpcs()
    {
        // 岚婆婆 (浅滩) — first objective NPC.
        WireNpc("GrannyLan", DialogueData.GrannyLan, DialogueData.SpeakerGranny,
            SX(new Vector2(520, 360)), Palette.WarningAmber, 124f,
            () => _objectives?.OnGrannyTalked());
        // 海星 (浅滩) — deepened on repeat talk.
        WireNpc("StarfishNPC", DialogueData.Starfish, DialogueData.SpeakerStarfish,
            SX(new Vector2(980, 320)), Palette.CoastalCyan, 108f,
            () => _objectives?.OnStarfishTalked(), NarrativeData.StarfishDeep);
        // 海草 (浅滩/沉积带 border) — deepened on repeat talk.
        WireNpc("SeaweedNPC", DialogueData.Seaweed, DialogueData.SpeakerSeaweed,
            SX(new Vector2(700, 840)), Palette.PollutedTealBright, 108f,
            () => _objectives?.OnSeaweedTalked(), NarrativeData.SeaweedDeep);

        // NEW in-canon NPCs (item 6), placed across zones, added as runtime nodes.
        WireRuntimeNpc("HermitNPC", NarrativeData.Hermit, SX(new Vector2(1700, 760)),
            new Color(0.7f, 0.5f, 0.35f), "hermit", DialogueData.SpeakerHermit);
        WireRuntimeNpc("ShoalNPC", NarrativeData.Shoal, SX(new Vector2(2150, 500)),
            new Color(0.5f, 0.7f, 0.8f), "shoal", DialogueData.SpeakerShoal);
        WireRuntimeNpc("LanternNPC", NarrativeData.Lantern, SX(new Vector2(2900, 420)),
            new Color(0.85f, 0.8f, 0.45f), "lantern", DialogueData.SpeakerLantern);

        // Residents painted for chapter one but not hand-placed above.
        foreach (var member in ChapterCast.For(1))
            WireRuntimeNpc($"Cast_{member.Id}", member.Id, SX(member.Position),
                member.Tint, member.PortraitId, member.DisplayName, member.DisplaySize);

        // 汽油桶 (浅滩) — examinable canon prop with its vignette.
        var barrel = GetNodeOrNull<Node2D>("GasolineBarrel");
        if (barrel != null)
        {
            barrel.Position = SX(new Vector2(1100, 860));
            var barrelSprite = new Sprite2D
            {
                Texture = AssetLoader.Texture(AssetLoader.NpcPortrait("barrel"))
                          ?? PlaceholderArt.RoundBlob(64, new Color(0.5f, 0.45f, 0.3f)),
            };
            PlaceholderArt.FitSprite(barrelSprite, 120f);
            barrel.AddChild(barrelSprite);
            _interactables.Add(new Interactable
            {
                Node = barrel,
                Timeline = DialogueData.JellyfishBox,
                Prompt = AddWorldPrompt(barrel, "E", "Examine"),
            });
        }
    }

    private void WireNpc(string nodeName, string timeline, string displayName, Vector2 pos,
        Color tint, float displaySize, System.Action onDone, string? followUp = null)
    {
        var node = GetNodeOrNull<Node2D>(nodeName);
        if (node == null) return;
        node.Position = pos;

        var portrait = AssetLoader.Texture(AssetLoader.NpcPortrait(timeline));
        var npcSprite = new Sprite2D { Texture = portrait ?? PlaceholderArt.RoundBlob(80, tint) };
        if (portrait != null) PlaceholderArt.FitSprite(npcSprite, displaySize);
        node.AddChild(npcSprite);
        var identity = DecorateNpc(node, npcSprite, displayName, tint);

        _interactables.Add(new Interactable
        {
            Node = node,
            Timeline = timeline,
            OnFirstDone = onDone,
            FollowUp = followUp,
            Identity = identity,
            Prompt = AddWorldPrompt(node, "E", "Talk"),
        });
    }

    private void WireRuntimeNpc(string name, string timeline, Vector2 pos, Color tint,
        string portraitId, string displayName, float displaySize = 112f)
    {
        var node = new Node2D { Name = name, Position = pos };
        node.AddToGroup("npc");
        AddChild(node);
        var portrait = AssetLoader.Texture(AssetLoader.NpcPortrait(portraitId));
        var runtimeSprite = new Sprite2D { Texture = portrait ?? PlaceholderArt.RoundBlob(80, tint) };
        if (portrait != null) PlaceholderArt.FitSprite(runtimeSprite, displaySize);
        node.AddChild(runtimeSprite);
        var identity = DecorateNpc(node, runtimeSprite, displayName, tint);
        _interactables.Add(new Interactable
        {
            Node = node,
            Timeline = timeline,
            Identity = identity,
            Prompt = AddWorldPrompt(node, "E", "Talk"),
        });
    }

    /// A shared NPC signature: larger portrait, breathing halo, persistent name and
    /// a nearby-only action prompt. It distinguishes characters from decorative
    /// creatures without covering the painted scene with permanent interaction UI.
    private static Control DecorateNpc(Node2D node, Sprite2D sprite, string displayName, Color accent)
    {
        node.ZIndex = 5;

        var points = new Vector2[33];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.Tau * i / (points.Length - 1);
            float inkWobble = Mathf.Sin(angle * 5f + node.GetInstanceId() % 11) * 2.8f
                            + Mathf.Sin(angle * 9f) * 1.2f;
            points[i] = Vector2.FromAngle(angle) * (70f + inkWobble);
        }
        var halo = new Line2D
        {
            Name = "NpcHalo",
            Points = points,
            Width = 2.5f,
            DefaultColor = new Color(accent.R, accent.G, accent.B, 0.58f),
            Antialiased = true,
            ZIndex = -1,
        };
        node.AddChild(halo);
        var haloTween = halo.CreateTween().SetLoops();
        haloTween.TweenProperty(halo, "modulate:a", 0.28f, 1.5f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        haloTween.TweenProperty(halo, "modulate:a", 0.92f, 1.5f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        var name = UiTheme.WorldRole(UiTheme.TypeRole.Name, displayName);
        name.Name = "NpcName";
        name.Position = new Vector2(-150, -108);
        name.Size = new Vector2(300, 30);
        node.AddChild(name);

        float baseY = sprite.Position.Y;
        float duration = 1.65f + (node.GetInstanceId() % 7) * 0.09f;
        var bob = sprite.CreateTween().SetLoops();
        bob.TweenProperty(sprite, "position:y", baseY - 6f, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        bob.TweenProperty(sprite, "position:y", baseY + 6f, duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        return name;
    }

    /// The shared key-cap prompt (UiTheme.KeyPrompt), centred under a world object.
    /// Chapter one used a glass pill with 13px regular text while chapters two and
    /// three used a bare label; both now read the same way.
    private static Control AddWorldPrompt(Node2D node, string key, string action)
    {
        var holder = new CenterContainer
        {
            Name = "InteractionPrompt",
            Position = new Vector2(-150, 78), Size = new Vector2(300, 36),
            Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        holder.AddChild(UiTheme.KeyPrompt(key, action));
        node.AddChild(holder);
        return holder;
    }

    private void SetupLoreObjects()
    {
        // Scatter 残片笔记 across zones (item 5). The barrel above is the canon example;
        // these add several more in-canon readable objects.
        var positions = new System.Collections.Generic.Dictionary<string, Vector2>
        {
            ["lore_net"] = SX(new Vector2(300, 700)),
            ["lore_bottle"] = SX(new Vector2(1180, 300)),
            ["lore_pipe"] = SX(new Vector2(1620, 880)),
            ["lore_shell"] = SX(new Vector2(2000, 760)),
            ["lore_log"] = SX(new Vector2(2300, 280)),
            ["lore_membrane"] = SX(new Vector2(2800, 820)),
            ["lore_lantern"] = SX(new Vector2(3250, 560)),
        };
        foreach (var note in NarrativeData.LoreNotes)
        {
            if (note.Id == "lore_barrel") continue; // represented by the barrel prop
            if (!positions.TryGetValue(note.Id, out var pos)) continue;
            var node = new Node2D { Name = "Lore_" + note.Id, Position = pos };
            node.AddToGroup("lore");
            var noteTex = AssetLoader.Texture(AssetLoader.NoteIcon)
                          ?? PlaceholderArt.RoundBlob(40, new Color(0.55f, 0.5f, 0.32f));
            var noteSprite = new Sprite2D { Texture = noteTex };
            if (AssetLoader.Has(AssetLoader.NoteIcon)) PlaceholderArt.FitSprite(noteSprite, 64f);
            node.AddChild(noteSprite);
            AddChild(node);
            _interactables.Add(new Interactable
            {
                Node = node,
                Note = note,
                Prompt = AddWorldPrompt(node, "E", "Examine"),
            });
        }
    }

    // --- Per-frame ---

    public override void _PhysicsProcess(double delta)
    {
        CheckExitReached();
        CheckZone();
    }

    public override void _Process(double delta)
    {
        UpdateInteractionPrompts();
        if (Input.IsActionJustPressed("e")) TryInteract();
    }

    private void UpdateInteractionPrompts()
    {
        if (_player == null) return;
        foreach (var it in _interactables)
        {
            float distance = it.Node.GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (it.Prompt != null)
                it.Prompt.Visible = !_dialogue.IsActive && distance <= 170f;
            if (it.Identity != null)
                it.Identity.Modulate = distance <= 260f ? Colors.White : new Color(1f, 1f, 1f, 0.78f);
        }
    }

    private void CheckZone()
    {
        if (_player == null) return;
        float x = _player.GlobalPosition.X;
        string zone = x >= ZoneDepthsX ? NarrativeData.ZoneDepths
            : x >= ZoneSedimentX ? NarrativeData.ZoneSediment
            : NarrativeData.ZoneShallows;
        if (zone == _currentZone) return;
        _currentZone = zone;
        _player.SetSafePoint(_player.GlobalPosition); // entering a zone = a safe beat
        Events.Instance?.EmitSignal(Events.SignalName.ZoneEntered, zone);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            string.Format(GameStrings.Tr("ZONE_ENTER_TEMPLATE"), zone));
    }

    private void TryInteract()
    {
        if (_player == null || _dialogue.IsActive) return;
        foreach (var it in _interactables)
        {
            if (it.Node.GlobalPosition.DistanceTo(_player.GlobalPosition) > 150f) continue;

            if (it.Note != null)
            {
                // 残片笔记 lore object — show vignette + record (item 5).
                MemoryLog.Instance?.RecordNote(it.Note.Id);
                Events.Instance?.EmitSignal(Events.SignalName.LoreNoteFound, it.Note.Id);
                Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                    string.Format(GameStrings.Tr("NOTE_FOUND_TEMPLATE"), it.Note.Title));
                _dialogue.ShowVignette(it.Note.Title, it.Note.Lines, "Fragment");
                return;
            }

            // NPC dialogue. First talk fires the objective callback; later talks play
            // the optional deepened follow-up (item 6) if one exists.
            bool firstTime = !it.Talked;
            it.Talked = true;
            string timeline = (!firstTime && it.FollowUp != null) ? it.FollowUp : it.Timeline;
            _dialogue.Play(timeline, firstTime ? it.OnFirstDone : null);
            return;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Entering pause happens here; leaving it belongs to the PauseInput autoload,
        // because this node is Pausable and stops receiving input once paused. The
        // event must be consumed or PauseInput would see it too and undo the pause.
        if (@event.IsActionPressed("pause_game"))
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("restart_level")) RestartLevel();
    }

    private void CheckExitReached()
    {
        if (_exitReached || _player == null) return;
        if (_objectives?.Stage != ObjectiveStage.ExitLevel) return;
        if (_player.GlobalPosition.DistanceTo(ChapterExitPoint) <= GameConstants.ExitReachDistance)
        {
            _exitReached = true;
            AudioManager.Instance?.PlaySfx("exit_reached");
            Events.Instance?.EmitSignal(Events.SignalName.LevelExitReached);
        }
    }

    private void TogglePause()
    {
        _paused = !GetTree().Paused;
        GetTree().Paused = _paused;
        AudioManager.Instance?.PlaySfx("pause_toggle");
        Events.Instance?.EmitSignal(Events.SignalName.GamePaused, _paused);
    }

    public void RestartLevel()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    // --- Narrative beats ---

    private void OnShardProgressChanged(int collected, int required)
    {
        if (collected >= 3) SetZoneRestored(1);
        // 岚婆婆 mid beat (item 6) fires once the player is collecting shards.
        if (!_grannyMidPlayed && collected == 1 && !_dialogue.IsActive)
        {
            _grannyMidPlayed = true;
            CallDeferred(nameof(PlayGrannyMid));
        }
    }

    private void OnObjectiveAdvanced(int stage)
    {
        if ((ObjectiveStage)stage >= ObjectiveStage.CollectShards)
            SetZoneRestored(0);
    }

    private void SetZoneRestored(int zone)
    {
        if (zone < 0 || zone >= _zoneRestored.Length || _zoneRestored[zone]) return;
        _zoneRestored[zone] = true;
        if (zone < _pollutionVeils.Count)
        {
            var veil = _pollutionVeils[zone];
            veil.CreateTween().TweenProperty(veil, "modulate:a", 0.15f, 1.8f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        }
        if (zone < _zoneInvaders.Count)
        {
            foreach (var invader in _zoneInvaders[zone])
            {
                var tween = invader.CreateTween();
                tween.TweenProperty(invader, "modulate:a", 0f, 1.4f)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                tween.TweenCallback(Callable.From(invader.QueueFree));
            }
        }
        if (zone < _zoneShortcutGates.Count)
        {
            foreach (var gate in _zoneShortcutGates[zone])
            {
                gate.CollisionLayer = 0;
                var tween = gate.CreateTween();
                tween.TweenProperty(gate, "modulate:a", 0f, 1.1f)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                tween.TweenCallback(Callable.From(gate.QueueFree));
            }
            _zoneShortcutGates[zone].Clear();
        }
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            zone switch
            {
                0 => "Plastic and lost gear can trap wildlife. The coral shortcut is clear.",
                1 => "Runoff can feed algal blooms and lower oxygen. The migration route is open.",
                _ => "Removing pollution helps. Stopping it upstream protects the nursery for longer.",
            });
    }

    private void PlayGrannyMid()
    {
        if (!_dialogue.IsActive) _dialogue.Play(NarrativeData.GrannyMid);
    }

    private void OnBossHealthChanged(int current, int max)
    {
        // Codex entry for the boss is unlocked the moment its health is first reported.
        MemoryLog.Instance?.UnlockCodex("codex_boss");

        if (!_bossPrePlayed && current == max)
        {
            _bossPrePlayed = true;
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, GameStrings.Tr("BOSS_PURIFY_HINT"));
        }
        else if (!_bossPrePlayed && current < max)
        {
            // First purification touch — play the pre/求救 beat.
            _bossPrePlayed = true;
            if (!_dialogue.IsActive) _dialogue.Play(NarrativeData.BossPre);
        }
        if (!_bossMidPlayed && current > 0 && current <= max / 2)
        {
            _bossMidPlayed = true;
            if (!_dialogue.IsActive) _dialogue.Play(NarrativeData.BossMid);
        }
    }

    private void OnMemoryUnlocked(int index)
    {
        // Play the corresponding 微光潮汐记忆 vignette (item 3). Skip if a beat is mid-play
        // to avoid stomping a conversation; the memory is still recorded in the log.
        if (_dialogue.IsActive) return;
        var mem = NarrativeData.MemoryAt(index);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            string.Format(GameStrings.Tr("MEMORY_UNLOCKED_TEMPLATE"), mem.Title));
        _dialogue.ShowVignette(mem.Title, mem.Lines, DialogueData.SpeakerShimmer);
    }

    // --- Save / restore ---

    private void OnBossDefeated()
    {
        _bossDefeated = true;
        SetZoneRestored(2);
        if (_exitMarker != null)
            _exitMarker.CreateTween().TweenProperty(_exitMarker, "modulate", Colors.White, 1.0f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint, GameStrings.Tr("BOSS_PURIFIED"));
    }

    private void OnNextLevelElementUnlocked(int form)
    {
        _unlockedNext = (ElementForm)form;
        // Post beats: 巢母净化 → 岚婆婆 收尾 → 结章·潮汐钥匙 (item 6).
        _dialogue.Play(DialogueData.BossDefeated,
            () => _dialogue.Play(NarrativeData.GrannyAfter,
                () => _dialogue.Play(NarrativeData.Ending)));
    }

    private void OnObjectiveCompleted() => PersistProgress();

    private void PersistProgress()
    {
        if (_player == null || _objectives == null) return;
        var state = new SaveState(
            Version: SaveManager.SaveVersion,
            SavedAt: Time.GetDatetimeStringFromSystem(true),
            CurrentForm: _player.CurrentForm,
            CurrentHealth: _player.CurrentHealth,
            WaterCharges: 0, IceCharges: 0, ElectricCharges: 0,
            ObjectiveStage: _objectives.Stage,
            NextLevelElementUnlocked: _unlockedNext == ElementForm.Base ? null : _unlockedNext,
            FirstBossDefeated: _bossDefeated,
            FirstWaterCollected: _objectives.ShardsCollected > 0,
            CurrentLevel: 1,
            CollectedCount: _objectives.ShardsCollected);
        SaveManager.Instance?.SaveState(state);
    }

    private void RestoreProgressIfSaved()
    {
        var state = SaveManager.Instance?.LoadState();
        if (state == null) return;
        _player?.RestoreState(state.CurrentHealth, state.CurrentForm);
        _skills?.RestoreCharges(state.WaterCharges, state.IceCharges, state.ElectricCharges);
        _objectives?.RestoreStage(state.ObjectiveStage, 0);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            string.Format(GameStrings.Tr("RESUME_NOTICE_TEMPLATE"), state.WaterCharges));
    }
}

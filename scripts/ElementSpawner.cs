using Godot;

namespace ShallowSeaDream;

/// res://scripts/ElementSpawner.cs
/// Spawns water-shard pickups across the map grid and handles pickup detection
/// (split from the god-class). Each shard carries a typed ElementForm rather than
/// the original "冰"/"电" node-name heuristic.
public partial class ElementSpawner : Node2D
{
    [Export] public float PickupDistance = GameConstants.ElementPickupDistance;
    /// How many shards to keep available on the map at once.
    /// The map grew from 3600 to ~17200 wide, so the old count of 8 left long empty
    /// stretches. This keeps roughly the same pickup density per screen.
    [Export] public int ActiveShardTarget = 22;

    private Player? _player;
    private SkillSystem? _skills;
    private ObjectiveManager? _objectives;
    private bool _firstWaterCollected;
    private float _bob;
    private float _lockedHintCooldown;
    private int _memoryIndex; // each pickup unlocks the next 微光潮汐记忆 (item 3)

    // Full-map grid spawn points (port WATER_ELEMENT_SPAWN_POINTS).
    private readonly System.Collections.Generic.List<Vector2> _spawnPoints = new();
    private readonly System.Collections.Generic.List<Sprite2D> _activeShards = new();
    private readonly System.Collections.Generic.Dictionary<Sprite2D, Vector2> _shardOrigins = new();
    private readonly System.Collections.Generic.Dictionary<Sprite2D, float> _shardPhases = new();
    private int _spawnCursor;

    /// On-screen size of a collectable. The pickup is the chapter's pollution item, so
    /// it has to read as an object, not a mote — but never larger than a resident
    /// (the smallest is 126px), or the litter outranks the characters.
    private const float ShardDisplaySize = 96f;

    /// Keep a pickup this far from any character or prop centre.
    private const float PickupClearance = 190f;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        _skills = GetParent().GetNodeOrNull<SkillSystem>("SkillSystem");
        _objectives = GetParent().GetNodeOrNull<ObjectiveManager>("ObjectiveManager");
        BuildSpawnGrid();
        // The level root wires its NPCs, props and lore notes in its own _Ready, which
        // runs after this child's. Defer the first stocking so those already exist and
        // pickups can avoid landing on them.
        CallDeferred(nameof(StockInitial));
    }

    /// Authored branch rewards for the organic maze. Unlike the old regular grid,
    /// these points sit in corridors and side pockets, never inside a reef obstacle.
    private void BuildSpawnGrid()
    {
        var authored = new[]
        {
            // 浅滩: two on the readable route, three in optional pockets.
            new Vector2(190, 200), new Vector2(250, 860), new Vector2(560, 560),
            new Vector2(820, 220), new Vector2(900, 850), new Vector2(1220, 520),
            // 沉积带: alternating upper/lower branches around the central reef spine.
            new Vector2(1340, 480), new Vector2(1560, 220), new Vector2(1710, 900),
            new Vector2(1960, 900), new Vector2(2140, 430), new Vector2(2420, 820),
            // 巢母深处: the safest pickups trace the long route toward the arena.
            new Vector2(2540, 520), new Vector2(2680, 170), new Vector2(2880, 500),
            new Vector2(2960, 900), new Vector2(3160, 260), new Vector2(3380, 620),
            new Vector2(3480, 900),
            // Infill so the long crossing never has a bare screen.
            new Vector2(400, 400), new Vector2(680, 760), new Vector2(1020, 340),
            new Vector2(1100, 700), new Vector2(1450, 800), new Vector2(1620, 620),
            new Vector2(1840, 300), new Vector2(2060, 660), new Vector2(2280, 200),
            new Vector2(2600, 760), new Vector2(2780, 320), new Vector2(3060, 560),
            new Vector2(3280, 840), new Vector2(3540, 300),
        };
        // Authored against ChapterMap.AuthoredWidth; restretch onto the long map.
        foreach (var point in authored)
            _spawnPoints.Add(ChapterMap.Place(ChapterRuntime.CurrentChapter, point));
    }

    public override void _Process(double delta)
    {
        _bob += (float)delta * 3f;
        _lockedHintCooldown = Mathf.Max(0f, _lockedHintCooldown - (float)delta);
        for (int i = 0; i < _activeShards.Count; i++)
        {
            var shard = _activeShards[i];
            if (_shardOrigins.TryGetValue(shard, out var origin))
                shard.Position = origin + new Vector2(0, Mathf.Sin(_bob + _shardPhases[shard]) * 8f);
        }

        if (_player == null) return;
        for (int i = _activeShards.Count - 1; i >= 0; i--)
        {
            var shard = _activeShards[i];
            if (shard.GlobalPosition.DistanceTo(_player.GlobalPosition) <= PickupDistance)
            {
                if (_objectives != null && _objectives.Stage < ObjectiveStage.CollectShards)
                {
                    if (_lockedHintCooldown <= 0f)
                    {
                        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                            "Listen to the residents before you collect the scattered Tidal Memories.");
                        _lockedHintCooldown = 2f;
                    }
                    continue;
                }
                _activeShards.RemoveAt(i);
                _shardOrigins.Remove(shard);
                _shardPhases.Remove(shard);
                shard.QueueFree();
                CollectShard(ElementForm.Water);
                SpawnNext(); // keep the map stocked
            }
        }
    }

    private void StockInitial()
    {
        for (int i = 0; i < ActiveShardTarget; i++) SpawnNext();
    }

    private void SpawnNext()
    {
        if (_spawnPoints.Count == 0) return;
        // Walk forward past any point currently covered by a character or prop, so a
        // pickup never renders on top of one. One full lap is the give-up condition.
        for (int tries = 0; tries < _spawnPoints.Count; tries++)
        {
            var candidate = _spawnPoints[_spawnCursor % _spawnPoints.Count];
            _spawnCursor++;
            if (!ChapterMap.IsClearOfOccupants(this, candidate, PickupClearance, "npc", "lore", "box"))
                continue;
            SpawnShard(ElementForm.Water, candidate);
            return;
        }
    }

    /// Spawn a single shard of the given form at a clear grid point.
    public void SpawnShard(ElementForm form, Vector2 position)
    {
        var shard = new Sprite2D
        {
            Name = "WaterShard",
            Position = position,
            // Shards are now 微光潮汐记忆 motes (item 3): prefer the memory icon, then the
            // water-shard icon, then a procedural blob.
            Texture = AssetLoader.Texture(AssetLoader.ChapterElement(ChapterRuntime.CurrentChapter))
                      ?? AssetLoader.Texture(AssetLoader.MemoryIcon)
                      ?? AssetLoader.Texture(AssetLoader.ShardIcon)
                      ?? PlaceholderArt.RoundBlob(40, Palette.ForForm(form)),
        };
        // Scale to a readable in-world pickup. The source art is up to 1024px; without
        // this the shard renders at native size — giant overlapping glow orbs that read as
        // a halftone/dot artifact across the field.
        if (shard.Texture != null)
        {
            float longest = Mathf.Max(shard.Texture.GetWidth(), shard.Texture.GetHeight());
            if (longest > 0) shard.Scale = Vector2.One * (ShardDisplaySize / longest);
        }
        shard.AddToGroup("element");
        AddChild(shard);
        _activeShards.Add(shard);
        _shardOrigins[shard] = position;
        _shardPhases[shard] = (float)GD.RandRange(0.0, Mathf.Tau);
    }

    private void CollectShard(ElementForm form)
    {
        if (form == ElementForm.Water && !_firstWaterCollected)
        {
            _firstWaterCollected = true;
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, GameStrings.Tr("STATUS_FIRST_WATER"));
        }
        _skills?.AddCharge(form);
        AudioManager.Instance?.PlaySfx("element_pickup");
        Events.Instance?.EmitSignal(Events.SignalName.ElementPickedUp, (int)form);

        // Item 3 — collecting a shard is now a narrative act: unlock the next memory
        // vignette and record it in the log. GameSceneController plays it via dialogue.
        if (form == ElementForm.Water && _memoryIndex < NarrativeData.MemoryCount)
        {
            var mem = NarrativeData.MemoryAt(_memoryIndex);
            MemoryLog.Instance?.UnlockMemory(mem.Id);
            Events.Instance?.EmitSignal(Events.SignalName.MemoryUnlocked, _memoryIndex);
            _memoryIndex++;
        }
    }
}

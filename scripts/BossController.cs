using Godot;

namespace ShallowSeaDream;

/// res://scripts/BossController.cs
/// The 潮涡巢母 / Tide-Vortex Brood Mother (split from the god-class + the original
/// pollution_monster / pollution_monster_base pair, now unified). Its weakness,
/// painting and next-chapter key are configured per scene.
public partial class BossController : CharacterBody2D
{
    [Signal] public delegate void DefeatedEventHandler();

    [Export] public string MonsterName = GameConstants.MonsterBossName;
    [Export] public int MaxHealthValue = GameConstants.BossMaxHealth;
    [Export] public int AttackDamage = GameConstants.BossAttackDamage;
    [Export] public float AutoAttackRange = GameConstants.BossAutoAttackRange;
    [Export] public float AutoAttackInterval = GameConstants.BossAutoAttackInterval;
    [Export] public ElementForm EffectiveElement = ElementForm.Water;
    [Export] public string SpritePath = AssetLoader.BossSheet;
    [Export] public float DisplaySize = 220f;
    [Export] public ElementForm NextUnlockedElement = ElementForm.Ice;
    [Export] public bool EmitNextElementUnlock = true;

    public int CurrentHealth { get; private set; } = GameConstants.BossMaxHealth;
    public bool IsDefeated { get; private set; }
    /// Set true once the player has come close enough to read the codex profile.
    public bool ProfileViewed { get; private set; }

    private float _attackCooldown;
    private Player? _player;
    private AnimatedSprite2D? _sprite;
    private float _pulse;
    private bool _dialogueActive;

    public override void _Ready()
    {
        AddToGroup("monster");
        CurrentHealth = MaxHealthValue;
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (Events.Instance != null)
        {
            Events.Instance.DialogueStarted += _ => _dialogueActive = true;
            Events.Instance.DialogueFinished += _ => _dialogueActive = false;
        }
        BuildVisual();
        Events.Instance?.EmitSignal(Events.SignalName.BossHealthChanged, CurrentHealth, MaxHealthValue);
    }

    private void BuildVisual()
    {
        _sprite = new AnimatedSprite2D { Name = "BossSprite" };
        var tex = AssetLoader.Texture(SpritePath);
        _sprite.SpriteFrames = tex != null
            ? PlaceholderArt.SliceCreatureSheet(tex, 3, 2)
            : PlaceholderArt.BlobFrames(Palette.PollutedTeal, 180);
        var frame = _sprite.SpriteFrames.GetFrameTexture("Idle", 0);
        if (frame != null)
        {
            float longest = Mathf.Max(frame.GetWidth(), frame.GetHeight());
            if (longest > 0f) _sprite.Scale = Vector2.One * (DisplaySize / longest);
        }
        _sprite.Play("Idle");
        AddChild(_sprite);

        var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = 58f } };
        AddChild(shape);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Pulsing arena presentation (procedural — DESIGN_BRIEF §2 layer 2).
        _pulse += (float)delta * 2.0f;
        if (_sprite != null)
        {
            // Keep the brood-mother illustration bright; pulse only a light teal sheen
            // over near-white so the art stays readable instead of washed dark.
            var sheen = Palette.PollutedTeal.Lerp(Palette.PollutedTealBright, 0.5f + 0.5f * Mathf.Sin(_pulse));
            _sprite.Modulate = Colors.White.Lerp(sheen, 0.35f);
        }

        if (IsDefeated || _player == null || _dialogueActive) return;

        float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
        if (!ProfileViewed && dist <= AutoAttackRange + 120f)
        {
            ProfileViewed = true;
            string hint = ChapterRuntime.CurrentChapter == 2
                ? "霜壳正在替海沟承受污染；用冰元素共鸣，别把它击碎。"
                : ChapterRuntime.CurrentChapter == 3
                    ? "炉心的热量已经失控；用电元素把能量导回安全回路。"
                    : GameStrings.Tr("STATUS_BOSS_GATE");
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, hint);
        }

        _attackCooldown -= (float)delta;
        if (_attackCooldown <= 0f && dist <= AutoAttackRange)
        {
            PerformAttack();
            _attackCooldown = AutoAttackInterval;
        }
    }

    public bool IsSkillEffective(ElementForm form) => form == EffectiveElement;

    /// Apply purification damage from a skill cast.
    public void ApplyDamage(int amount)
    {
        if (IsDefeated) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Events.Instance?.EmitSignal(Events.SignalName.BossHealthChanged, CurrentHealth, MaxHealthValue);
        AudioManager.Instance?.PlaySfx("boss_hit");
        if (CurrentHealth == 0)
            Defeat();
    }

    private void PerformAttack()
    {
        _player?.TakeDamage(AttackDamage);
        Events.Instance?.EmitSignal(Events.SignalName.BossAttacked, AttackDamage);
        AudioManager.Instance?.PlaySfx("boss_attack");
    }

    private void Defeat()
    {
        IsDefeated = true;
        if (_sprite != null) _sprite.Modulate = Palette.CoastalCyan;
        EmitSignal(SignalName.Defeated);
        Events.Instance?.EmitSignal(Events.SignalName.BossDefeated);
        AudioManager.Instance?.PlaySfx("boss_defeat");
        if (EmitNextElementUnlock)
        {
            SpawnNextLevelElementDrop();
            Events.Instance?.EmitSignal(Events.SignalName.NextLevelElementUnlocked, (int)NextUnlockedElement);
        }
    }

    /// Float the next chapter's elemental key at the guardian position.
    private void SpawnNextLevelElementDrop()
    {
        var orb = new Sprite2D
        {
            Name = $"{GameStrings.FormLabel(NextUnlockedElement)}OrbDrop",
            Texture = AssetLoader.Texture(AssetLoader.MemoryIcon)
                      ?? PlaceholderArt.RoundBlob(64, Palette.ForForm(NextUnlockedElement)),
            GlobalPosition = GlobalPosition,
            Modulate = Palette.ForForm(NextUnlockedElement),
        };
        PlaceholderArt.FitSprite(orb, 76f);
        GetParent().AddChild(orb);
        var tween = orb.CreateTween().SetLoops();
        tween.TweenProperty(orb, "position:y", orb.Position.Y - 24f, 1.2f);
        tween.TweenProperty(orb, "position:y", orb.Position.Y, 1.2f);
    }
}

using Godot;

namespace ShallowSeaDream;

/// res://scripts/BossController.cs
/// Pollution monster controller (split from the god-class + the original
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
    /// Chapters two and three open their arena after the third restoration node, but
    /// the guardian stays invulnerable until the player has cleaned enough smaller
    /// pollution objects to store cleanup power.
    public bool CombatEnabled { get; set; } = true;
    /// Hostility and vulnerability are separate. The guardian begins attacking as
    /// soon as its arena opens, even while the player is still gathering enough
    /// cleanup charge to purify it.
    public bool AggressionEnabled { get; set; } = true;
    public Node2D? NextElementDrop { get; private set; }
    /// Set true once the player has come close enough to read the codex profile.
    public bool ProfileViewed { get; private set; }

    private float _attackCooldown;
    private Player? _player;
    private AnimatedSprite2D? _sprite;
    private CollisionShape2D? _bodyShape;
    private float _pulse;
    private float _dropPulse;
    private Vector2 _dropRestPosition;
    private bool _dialogueActive;
    private Vector2 _spriteBaseScale = Vector2.One;
    public float EffectiveAttackRange => Mathf.Max(AutoAttackRange, DisplaySize * 1.12f);
    public float EffectiveWarningRange => EffectiveAttackRange + Mathf.Max(180f, DisplaySize * 0.32f);

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
        // Player collision mask includes layer 32. Giving the guardian that layer
        // makes it a real obstacle: Shimmer cannot swim through its painting to loot
        // the far side of the arena.
        CollisionLayer = InteractionSpacing.WorldCollisionLayer;
        CollisionMask = 0;
        ZIndex = 20;
        _sprite = new AnimatedSprite2D { Name = "BossSprite" };
        var tex = AssetLoader.Texture(SpritePath);
        _sprite.SpriteFrames = tex != null
            ? PlaceholderArt.CreatureFrames(tex)
            : PlaceholderArt.BlobFrames(Palette.PollutedTeal, 180);
        var frame = _sprite.SpriteFrames.GetFrameTexture("Idle", 0);
        if (frame != null)
        {
            float longest = Mathf.Max(frame.GetWidth(), frame.GetHeight());
            if (longest > 0f) _sprite.Scale = Vector2.One * (DisplaySize / longest);
        }
        _spriteBaseScale = _sprite.Scale;
        _sprite.Play("Idle");
        AddChild(_sprite);

        // Scale with the drawing: a fixed 58px body left the enlarged guardians with a
        // hitbox floating in the middle of their silhouette.
        _bodyShape = new CollisionShape2D { Shape = new CircleShape2D { Radius = DisplaySize * 0.38f } };
        AddChild(_bodyShape);
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateNextElementDrop((float)delta);

        // Pulsing arena presentation (procedural — DESIGN_BRIEF §2 layer 2).
        _pulse += (float)delta * 2.0f;
        if (_sprite != null && !IsDefeated)
        {
            // Keep the brood-mother illustration bright; pulse only a light teal sheen
            // over near-white so the art stays readable instead of washed dark.
            var sheen = Palette.PollutedTeal.Lerp(Palette.PollutedTealBright, 0.5f + 0.5f * Mathf.Sin(_pulse));
            _sprite.Modulate = Colors.White.Lerp(sheen, 0.35f);
        }

        if (!AggressionEnabled || IsDefeated || _player == null || _dialogueActive) return;

        float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
        if (!ProfileViewed && dist <= EffectiveWarningRange)
        {
            ProfileViewed = true;
            string hint = ChapterRuntime.CurrentChapter == 2
                ? "Danger ahead. Keep distance; press 2 if it hits you."
                : ChapterRuntime.CurrentChapter == 3
                    ? "Danger ahead. Keep distance; press 3 if it hits you."
                    : "Danger ahead. Keep distance; press 1 if it hits you.";
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, hint);
        }

        _attackCooldown -= (float)delta;
        if (_attackCooldown <= 0f && dist <= EffectiveAttackRange)
        {
            PerformAttack();
            _attackCooldown = AutoAttackInterval;
        }
    }

    public bool IsSkillEffective(ElementForm form) => form == EffectiveElement;

    /// Apply purification damage from a skill cast.
    public void ApplyDamage(int amount)
    {
        if (!CombatEnabled || IsDefeated) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Events.Instance?.EmitSignal(Events.SignalName.BossHealthChanged, CurrentHealth, MaxHealthValue);
        AudioManager.Instance?.PlaySfx("boss_hit");
        if (CurrentHealth == 0)
            Defeat();
    }

    private void PerformAttack()
    {
        AnimateAttack();
        _player?.TakeDamage(AttackDamage);
        Events.Instance?.EmitSignal(Events.SignalName.BossAttacked, AttackDamage);
        Events.Instance?.EmitSignal(Events.SignalName.CombatHint, AttackInstruction());
        AudioManager.Instance?.PlaySfx("boss_attack");
    }

    private string AttackInstruction() => EffectiveElement switch
    {
        ElementForm.Ice => "HIT! Back up, then press 2 to seal it with cleanup power.",
        ElementForm.Electric => "HIT! Back up, then press 3 to pull it apart.",
        _ => "HIT! Back up, then press 1 to wash the plastic apart.",
    };

    private void AnimateAttack()
    {
        if (_sprite == null || _player == null) return;
        Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
        var restPos = _sprite.Position;
        var tween = _sprite.CreateTween();
        tween.TweenProperty(_sprite, "position", restPos - dir * 18f, 0.10f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_sprite, "scale", _spriteBaseScale * 1.10f, 0.10f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_sprite, "position", restPos + dir * 38f, 0.16f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(_sprite, "modulate", new Color(1f, 0.68f, 0.58f, 1f), 0.08f);
        tween.TweenProperty(_sprite, "position", restPos, 0.22f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_sprite, "scale", _spriteBaseScale, 0.22f);
        tween.Parallel().TweenProperty(_sprite, "modulate", Colors.White, 0.22f);
    }

    private void Defeat()
    {
        IsDefeated = true;
        AggressionEnabled = false;
        CombatEnabled = false;
        CollisionLayer = 0;
        CollisionMask = 0;
        if (_bodyShape != null)
            _bodyShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        EmitSignal(SignalName.Defeated);
        Events.Instance?.EmitSignal(Events.SignalName.BossDefeated);
        AudioManager.Instance?.PlaySfx("boss_defeat");
        if (EmitNextElementUnlock)
        {
            PlayDefeatAndSpawnDrop();
        }
    }

    private void PlayDefeatAndSpawnDrop()
    {
        if (_sprite == null)
        {
            SpawnNextLevelElementDrop();
            return;
        }

        var tween = _sprite.CreateTween();
        tween.Parallel().TweenProperty(_sprite, "modulate", new Color(Palette.CoastalCyan, 0f), 0.75f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_sprite, "scale", _spriteBaseScale * 0.72f, 0.75f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_sprite, "position:y", _sprite.Position.Y + 30f, 0.75f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.Finished += () =>
        {
            if (_sprite != null) _sprite.Visible = false;
            SpawnNextLevelElementDrop();
        };
    }

    /// Float the next chapter's elemental key above the restored guardian. The
    /// unlock signal is deliberately emitted on pickup, not on boss defeat: this
    /// makes the visible orb a real chapter key instead of decorative feedback.
    private void SpawnNextLevelElementDrop()
    {
        var parent = GetParent();
        if (parent == null || NextElementDrop != null) return;

        var drop = new Node2D
        {
            Name = $"{GameStrings.FormLabel(NextUnlockedElement)}OrbDrop",
            ZIndex = 80,
        };
        parent.AddChild(drop);
        float rise = Mathf.Clamp(DisplaySize * 0.42f, 140f, 240f);
        drop.GlobalPosition = GlobalPosition + Vector2.Up * rise;
        _dropRestPosition = drop.Position;

        Color elementColor = Palette.ForForm(NextUnlockedElement);

        var orb = new Sprite2D
        {
            Name = "ElementOrb",
            // ChapterElement is the chapter's pollution pickup (bottle/sludge/oil),
            // not the next-form key. Use the supplied painted clean-current orb here and
            // distinguish its element with colour plus a hand-drawn glyph.
            Texture = AssetLoader.Texture(AssetLoader.ShardIcon)
                      ?? AssetLoader.Texture(AssetLoader.MemoryIcon)
                      ?? PlaceholderArt.RoundBlob(96, elementColor),
            Modulate = Colors.White.Lerp(elementColor, 0.24f),
        };
        PlaceholderArt.FitSprite(orb, 132f);
        drop.AddChild(orb);
        drop.AddChild(MakeElementGlyph(NextUnlockedElement, elementColor));

        var label = UiTheme.WorldRole(UiTheme.TypeRole.Name,
            $"{GameStrings.FormLabel(NextUnlockedElement)} CURRENT");
        label.Position = new Vector2(-240f, 105f);
        label.Size = new Vector2(480f, 44f);
        drop.AddChild(label);
        var hint = UiTheme.WorldRole(UiTheme.TypeRole.Hint, "SWIM CLOSE TO COLLECT");
        hint.Position = new Vector2(-240f, 145f);
        hint.Size = new Vector2(480f, 36f);
        drop.AddChild(hint);

        NextElementDrop = drop;
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            $"A clean {GameStrings.FormLabel(NextUnlockedElement)} current appeared. Swim close to follow it.");
    }

    private void UpdateNextElementDrop(float delta)
    {
        if (NextElementDrop == null || !IsInstanceValid(NextElementDrop)) return;

        _dropPulse += delta;
        NextElementDrop.Position = _dropRestPosition + new Vector2(0f, Mathf.Sin(_dropPulse * 2.4f) * 12f);
        float pulse = 1f + Mathf.Sin(_dropPulse * 3.1f) * 0.045f;
        NextElementDrop.Scale = Vector2.One * pulse;

        if (!_dialogueActive && _player != null && NextElementDrop.GlobalPosition.DistanceTo(_player.GlobalPosition)
            <= GameConstants.ElementPickupDistance + 50f)
            CollectNextElementDrop();
    }

    /// Public for deterministic chapter-chain verification; normal play reaches this
    /// through proximity in UpdateNextElementDrop.
    public void CollectNextElementDrop()
    {
        if (NextElementDrop == null || !IsInstanceValid(NextElementDrop)) return;

        var drop = NextElementDrop;
        NextElementDrop = null;
        _player?.SetForm(NextUnlockedElement);
        Events.Instance?.EmitSignal(Events.SignalName.NextLevelElementUnlocked, (int)NextUnlockedElement);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            $"{GameStrings.FormLabel(NextUnlockedElement)} is ready. The way to the next sea is open.");
        AudioManager.Instance?.PlaySfx("objective_advance");

        var tween = drop.CreateTween();
        tween.Parallel().TweenProperty(drop, "scale", drop.Scale * 1.7f, 0.28f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(drop, "modulate:a", 0f, 0.28f);
        tween.Finished += drop.QueueFree;
    }

    private static Polygon2D MakeElementGlyph(ElementForm form, Color color)
    {
        Vector2[] points = form == ElementForm.Electric
            ? new[]
            {
                new Vector2(12,-48), new Vector2(-24,-4), new Vector2(-4,-4),
                new Vector2(-18,46), new Vector2(28,-14), new Vector2(7,-14),
            }
            : new[]
            {
                new Vector2(0,-42), new Vector2(14,-14), new Vector2(42,0),
                new Vector2(14,14), new Vector2(0,42), new Vector2(-14,14),
                new Vector2(-42,0), new Vector2(-14,-14),
            };
        return new Polygon2D
        {
            Name = "ElementGlyph",
            Polygon = points,
            Color = new Color(color.Lightened(0.22f), 0.94f),
            ZIndex = 2,
        };
    }
}

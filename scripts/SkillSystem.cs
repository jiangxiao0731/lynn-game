using Godot;

namespace ShallowSeaDream;

/// res://scripts/SkillSystem.cs
/// Elemental purification skills (split from the god-class). Tracks cleanup power
/// earned by treating smaller pollution objects, validates target, computes damage,
/// and fires the purification projectile.
/// Reads input actions skill_water/skill_ice/skill_electric and store_element.
public partial class SkillSystem : Node
{
    [Signal] public delegate void ChargesChangedEventHandler(int water, int ice, int electric);

    [Export] public float TargetSearchRadius = GameConstants.SkillTargetSearchRadius;

    private int _waterCharges;
    private int _iceCharges;
    private int _electricCharges;

    private Player? _player;

    public override void _Ready()
    {
        _player = GetParent().GetNodeOrNull<Player>("Player");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("skill_water")) TryCast(ElementForm.Water);
        else if (@event.IsActionPressed("skill_ice")) TryCast(ElementForm.Ice);
        else if (@event.IsActionPressed("skill_electric")) TryCast(ElementForm.Electric);
        else if (@event.IsActionPressed("store_element")) _player?.StoreForm();
    }

    /// Add cleanup power from a treated pollution object of the given form.
    public void AddCharge(ElementForm form, int amount = GameConstants.ElementChargesPerPickup)
    {
        switch (form)
        {
            case ElementForm.Water: _waterCharges += amount; break;
            case ElementForm.Ice: _iceCharges += amount; break;
            case ElementForm.Electric: _electricCharges += amount; break;
        }
        EmitCharges();
    }

    /// Re-apply persisted charges on resume.
    public void RestoreCharges(int water, int ice, int electric)
    {
        _waterCharges = water;
        _iceCharges = ice;
        _electricCharges = electric;
        EmitCharges();
    }

    /// Validate cleanup power + target, consume a charge, apply damage. Emits SkillCast/SkillFailed.
    public void TryCast(ElementForm form)
    {
        if (ChargesFor(form) <= 0)
        {
            Events.Instance?.EmitSignal(Events.SignalName.SkillFailed, "STATUS_NEED_ENERGY");
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, GameStrings.Tr("STATUS_NEED_ENERGY"));
            AudioManager.Instance?.PlaySfx("no_shards");
            return;
        }

        var target = FindTarget();
        if (target == null)
        {
            Events.Instance?.EmitSignal(Events.SignalName.SkillFailed, "STATUS_NEED_TARGET");
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint, GameStrings.Tr("STATUS_NEED_TARGET"));
            return;
        }

        // Every release also shifts Shimmer into that elemental form.
        _player?.SetForm(form);
        _player?.PlayCleanupAttack(form, target.GlobalPosition);

        bool effective = target.IsSkillEffective(form);
        int damage = effective ? GameConstants.DamageWaterEffective : GameConstants.DamageIneffective;
        SpendCharge(form);

        Events.Instance?.EmitSignal(Events.SignalName.SkillCast, (int)form, effective);
        string template = effective ? "SKILL_RELEASED_TEMPLATE" : "SKILL_INEFFECTIVE_TEMPLATE";
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            string.Format(GameStrings.Tr(template), GameStrings.FormLabel(form)));
        AudioManager.Instance?.PlaySfx("water_attack");

        LaunchProjectileAndDamage(target, damage, form);
    }

    /// Nearest active monster within its forgiving counter radius. Targeting must not
    /// depend on a separate codex/profile flag: that made the number key fail even
    /// while a large guardian was visibly attacking the player.
    private BossController? FindTarget()
    {
        if (_player == null) return null;
        BossController? best = null;
        float bestDist = float.MaxValue;
        foreach (var node in GetTree().GetNodesInGroup("monster"))
        {
            if (node is not BossController boss || !boss.CombatEnabled || boss.IsDefeated) continue;
            float d = boss.GlobalPosition.DistanceTo(_player.GlobalPosition);
            float allowed = Mathf.Max(TargetSearchRadius, boss.EffectiveCounterRange);
            if (d <= allowed && d <= bestDist)
            {
                bestDist = d;
                best = boss;
            }
        }
        return best;
    }

    /// Animate a water orb to the target over WaterAttackProjectileTime, then apply damage.
    private void LaunchProjectileAndDamage(BossController target, int damage, ElementForm form)
    {
        if (_player == null)
        {
            target.ApplyDamage(damage);
            return;
        }
        var orb = new Sprite2D
        {
            Texture = AssetLoader.Texture(AssetLoader.ShardIcon)
                      ?? PlaceholderArt.RoundBlob(48, Palette.ElementWaterImpact),
            GlobalPosition = _player.GlobalPosition,
            Modulate = Palette.ForForm(form),
        };
        PlaceholderArt.FitSprite(orb, 54f);
        GetParent().AddChild(orb);
        orb.Scale *= 0.65f;
        var tween = orb.CreateTween();
        // A brief gather lets the player's body motion read first; travel and growth
        // then happen together as one coherent purification pulse.
        tween.TweenInterval(0.09f);
        tween.TweenProperty(orb, "global_position", target.GlobalPosition, GameConstants.WaterAttackProjectileTime)
            .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(orb, "scale", orb.Scale * 1.45f, GameConstants.WaterAttackProjectileTime)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() =>
        {
            if (IsInstanceValid(target)) target.ApplyDamage(damage);
            orb.QueueFree();
        }));
    }

    private int ChargesFor(ElementForm form) => form switch
    {
        ElementForm.Water => _waterCharges,
        ElementForm.Ice => _iceCharges,
        ElementForm.Electric => _electricCharges,
        _ => 0,
    };

    private void SpendCharge(ElementForm form)
    {
        switch (form)
        {
            case ElementForm.Water: _waterCharges = Mathf.Max(0, _waterCharges - 1); break;
            case ElementForm.Ice: _iceCharges = Mathf.Max(0, _iceCharges - 1); break;
            case ElementForm.Electric: _electricCharges = Mathf.Max(0, _electricCharges - 1); break;
        }
        EmitCharges();
    }

    private void EmitCharges()
    {
        EmitSignal(SignalName.ChargesChanged, _waterCharges, _iceCharges, _electricCharges);
        Events.Instance?.EmitSignal(Events.SignalName.ElementChargesChanged, _waterCharges, _iceCharges, _electricCharges);
    }
}

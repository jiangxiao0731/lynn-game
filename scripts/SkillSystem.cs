using Godot;

namespace ShallowSeaDream;

/// res://scripts/SkillSystem.cs
/// Elemental purification skills (split from the god-class). Tracks per-element
/// charges, validates target + energy, computes damage, fires the water projectile.
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

    /// Add charges from a shard pickup of the given form.
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

    /// Validate energy + target, consume a charge, apply damage. Emits SkillCast/SkillFailed.
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

    /// Nearest monster within search radius whose profile has been viewed.
    private BossController? FindTarget()
    {
        if (_player == null) return null;
        BossController? best = null;
        float bestDist = TargetSearchRadius;
        foreach (var node in GetTree().GetNodesInGroup("monster"))
        {
            if (node is not BossController boss || boss.IsDefeated || !boss.ProfileViewed) continue;
            float d = boss.GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (d <= bestDist)
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
        tween.Parallel().TweenProperty(orb, "scale", orb.Scale * 1.45f, GameConstants.WaterAttackProjectileTime)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(orb, "global_position", target.GlobalPosition, GameConstants.WaterAttackProjectileTime);
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

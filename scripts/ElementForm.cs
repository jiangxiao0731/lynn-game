namespace ShallowSeaDream;

/// res://scripts/ElementForm.cs
/// Typed replacement for the original string-name heuristics
/// (FORM_BASE/FORM_WATER/... and the "冰"/"电" node-name parsing in get_element_form).
public enum ElementForm
{
    Base,
    Water,
    Ice,
    Electric,
}

/// The objective chain for Level 1 (ported from OBJECTIVE_* constants).
public enum ObjectiveStage
{
    FindNpc = 0,
    TalkStarfish = 1,
    TalkSeaweed = 2,
    CollectShards = 3,
    DefeatBoss = 4,
    ExitLevel = 5,
    Complete = 6,
}

/// Shared gameplay constants (ported from character_body_2d.gd / monster scripts).
public static class GameConstants
{
    public const float Speed = 240.0f;
    public const int MaxHealth = 120;

    public const int ElementChargesPerPickup = 1;
    public const float SkillTargetSearchRadius = 420.0f;
    public const int DamageWaterEffective = 52;
    public const int DamageIneffective = 18;

    public const float WaterAttackProjectileTime = 0.24f;
    public const float WaterAttackTargetRadius = 650.0f;

    public const float ElementPickupDistance = 100.0f;
    public const int ShardThresholdForBoss = 8;

    // Item 4 — gentle, narrative-driven purification (not an HP grind / harsh combat).
    public const float BossAutoAttackRange = 300.0f;
    public const float BossAutoAttackInterval = 3.2f;
    public const int BossMaxHealth = 208; // four deliberate water releases purify it
    public const int BossAttackDamage = 10; // low punishment, eco-fable tone

    public const float ExitReachDistance = 90.0f;
    // Exit sits deep in 巢母深处, past the purified brood mother (item 7).
    public static readonly Godot.Vector2 ExitPoint = new(3420, 540);

    public const string MonsterBossName = "潮涡巢母";
}

using Godot;

namespace ShallowSeaDream;

/// res://scripts/ObjectiveManager.cs
/// Drives the Level 1 objective chain (FindNpc → TalkStarfish → TalkSeaweed →
/// CollectShards → DefeatBoss → ExitLevel → Complete) and the 5-shard boss gate.
/// Split from the god-class; reacts to Events and re-broadcasts progress.
public partial class ObjectiveManager : Node
{
    [Signal] public delegate void StageChangedEventHandler(int stage);

    public ObjectiveStage Stage { get; private set; } = ObjectiveStage.FindNpc;
    public int ShardsCollected { get; private set; }

    public const int ShardThreshold = GameConstants.ShardThresholdForBoss;

    public override void _Ready()
    {
        var bus = Events.Instance;
        if (bus != null)
        {
            bus.ElementPickedUp += OnElementPickedUp;
            bus.BossDefeated += OnBossDefeated;
            bus.LevelExitReached += OnLevelExitReached;
        }
    }

    /// Push current stage + shard progress to the HUD (call after _Ready wiring).
    public void BroadcastInitial()
    {
        Events.Instance?.EmitSignal(Events.SignalName.ObjectiveAdvanced, (int)Stage);
        Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, ShardsCollected, ShardThreshold);
    }

    /// Advance to the next stage if the supplied stage matches the current one.
    public void AdvanceTo(ObjectiveStage next)
    {
        Stage = next;
        EmitSignal(SignalName.StageChanged, (int)next);
        Events.Instance?.EmitSignal(Events.SignalName.ObjectiveAdvanced, (int)next);
        AudioManager.Instance?.PlaySfx("objective_advance");
        if (next == ObjectiveStage.Complete)
            Events.Instance?.EmitSignal(Events.SignalName.ObjectiveCompleted);
    }

    /// Restore a persisted stage without re-firing the advance SFX.
    public void RestoreStage(ObjectiveStage stage, int shards)
    {
        Stage = stage;
        ShardsCollected = shards;
    }

    // --- NPC dialogue completions advance the early chain ---

    public void OnGrannyTalked()
    {
        if (Stage == ObjectiveStage.FindNpc) AdvanceTo(ObjectiveStage.TalkStarfish);
    }

    public void OnStarfishTalked()
    {
        if (Stage == ObjectiveStage.TalkStarfish) AdvanceTo(ObjectiveStage.TalkSeaweed);
    }

    public void OnSeaweedTalked()
    {
        if (Stage == ObjectiveStage.TalkSeaweed) AdvanceTo(ObjectiveStage.CollectShards);
    }

    private void OnElementPickedUp(int form)
    {
        if ((ElementForm)form != ElementForm.Water) return;
        ShardsCollected++;
        Events.Instance?.EmitSignal(Events.SignalName.ShardProgressChanged, ShardsCollected, ShardThreshold);
        if (ShardsCollected >= ShardThreshold && Stage == ObjectiveStage.CollectShards)
            AdvanceTo(ObjectiveStage.DefeatBoss);
    }

    private void OnBossDefeated()
    {
        if (Stage == ObjectiveStage.DefeatBoss)
            AdvanceTo(ObjectiveStage.ExitLevel);
    }

    private void OnLevelExitReached()
    {
        if (Stage == ObjectiveStage.ExitLevel)
            AdvanceTo(ObjectiveStage.Complete);
    }
}

using Godot;

namespace ShallowSeaDream;

/// res://scripts/Events.cs
/// Autoload signal bus (ported from Events.gd). Decouples Player / SkillSystem /
/// ObjectiveManager / HudController / ElementSpawner / BossController so they
/// communicate through Godot signals instead of the original god-class.
public partial class Events : Node
{
    // --- Player / health ---
    [Signal] public delegate void PlayerHealthChangedEventHandler(int current, int max);
    [Signal] public delegate void PlayerDamagedEventHandler(int amount);
    [Signal] public delegate void PlayerDefeatedEventHandler();
    [Signal] public delegate void PlayerFormChangedEventHandler(int form); // ElementForm

    // --- Skills / elements ---
    [Signal] public delegate void ElementChargesChangedEventHandler(int water, int ice, int electric);
    [Signal] public delegate void SkillCastEventHandler(int form, bool effective);
    [Signal] public delegate void SkillFailedEventHandler(string reason);
    [Signal] public delegate void ElementPickedUpEventHandler(int form);
    [Signal] public delegate void NextLevelElementUnlockedEventHandler(int form);

    // --- Objectives ---
    [Signal] public delegate void ObjectiveAdvancedEventHandler(int stage); // ObjectiveStage
    [Signal] public delegate void ShardProgressChangedEventHandler(int collected, int required);
    [Signal] public delegate void ObjectiveCompletedEventHandler();

    // --- Boss ---
    [Signal] public delegate void BossHealthChangedEventHandler(int current, int max);
    [Signal] public delegate void BossAttackedEventHandler(int damage);
    [Signal] public delegate void BossDefeatedEventHandler();

    // --- Narrative: memories / lore (items 3 & 5) ---
    [Signal] public delegate void MemoryUnlockedEventHandler(int memoryIndex);
    [Signal] public delegate void LoreNoteFoundEventHandler(string noteId);
    [Signal] public delegate void ZoneEnteredEventHandler(string zoneName);

    // --- Dialogue / NPC ---
    [Signal] public delegate void DialogueStartedEventHandler(string timelineId);
    [Signal] public delegate void DialogueLineStartedEventHandler(string speaker, string lineText);
    [Signal] public delegate void DialogueFinishedEventHandler(string timelineId);

    // --- Flow / level ---
    [Signal] public delegate void LevelExitReachedEventHandler();
    [Signal] public delegate void GamePausedEventHandler(bool paused);
    [Signal] public delegate void StatusHintEventHandler(string text);

    /// Convenience singleton accessor (set in _EnterTree). Autoload name is "Events".
    public static Events Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        Instance = this;
    }
}

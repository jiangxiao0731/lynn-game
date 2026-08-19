namespace ShallowSeaDream;

/// res://scripts/GameStrings.cs
/// Convenience facade over TranslationTable for typed lookups (form labels,
/// objective rows). Keeps call sites terse and centralizes the key strings.
public static class GameStrings
{
    public static string Tr(string key) => TranslationTable.Tr(key);

    public static string FormLabel(ElementForm form) => form switch
    {
        ElementForm.Water => Tr("FORM_WATER"),
        ElementForm.Ice => Tr("FORM_ICE"),
        ElementForm.Electric => Tr("FORM_ELECTRIC"),
        _ => Tr("FORM_BASE"),
    };

    public static string ObjectiveLabel(ObjectiveStage stage) => ChapterRuntime.ObjectiveLabel(stage);

    public static string LevelOneObjectiveLabel(ObjectiveStage stage) => stage switch
    {
        ObjectiveStage.FindNpc => Tr("OBJ_FIND_NPC"),
        ObjectiveStage.TalkStarfish => Tr("OBJ_TALK_STARFISH"),
        ObjectiveStage.TalkSeaweed => Tr("OBJ_TALK_SEAWEED"),
        ObjectiveStage.CollectShards => Tr("OBJ_COLLECT_SHARDS"),
        ObjectiveStage.DefeatBoss => Tr("OBJ_DEFEAT_BOSS"),
        ObjectiveStage.ExitLevel => Tr("OBJ_EXIT_LEVEL"),
        _ => Tr("OBJ_COMPLETE"),
    };

    /// All six objective rows in chain order (for the HUD checklist).
    public static readonly ObjectiveStage[] ChecklistOrder =
    {
        ObjectiveStage.FindNpc,
        ObjectiveStage.TalkStarfish,
        ObjectiveStage.TalkSeaweed,
        ObjectiveStage.CollectShards,
        ObjectiveStage.DefeatBoss,
        ObjectiveStage.ExitLevel,
    };
}

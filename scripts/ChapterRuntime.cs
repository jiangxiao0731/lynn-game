using Godot;

namespace ShallowSeaDream;

/// Small presentation context shared by the chapter controller and the reusable HUD.
/// It is set by a level root in _EnterTree, before child HUD nodes enter _Ready.
public static class ChapterRuntime
{
    public static int CurrentChapter { get; private set; } = 1;
    public static ElementForm RequiredForm { get; private set; } = ElementForm.Water;
    public static string FragmentLabel { get; private set; } = "Tidal Shards";
    public static string BossName { get; private set; } = GameConstants.MonsterBossName;
    public static string[] Zones { get; private set; } =
        { NarrativeData.ZoneShallows, NarrativeData.ZoneSediment, NarrativeData.ZoneDepths };

    public static void SetChapter(int chapter)
    {
        CurrentChapter = Mathf.Clamp(chapter, 1, 3);
        if (CurrentChapter == 2)
        {
            RequiredForm = ElementForm.Ice;
            FragmentLabel = "Frost Crystals";
            BossName = "Frostshell Guardian";
            Zones = new[] { "Frozen Inlet", "Ice Ridge", "Frost Nursery" };
        }
        else if (CurrentChapter == 3)
        {
            RequiredForm = ElementForm.Electric;
            FragmentLabel = "Circuit Sparks";
            BossName = "Overheated Core";
            Zones = new[] { "Wrecked Grid", "Heat Channel", "Silent Lighthouse" };
        }
        else
        {
            RequiredForm = ElementForm.Water;
            FragmentLabel = "Tidal Shards";
            BossName = GameConstants.MonsterBossName;
            Zones = new[] { NarrativeData.ZoneShallows, NarrativeData.ZoneSediment, NarrativeData.ZoneDepths };
        }
    }

    public static string ObjectiveLabel(ObjectiveStage stage)
    {
        if (CurrentChapter == 2)
            return stage switch
            {
                ObjectiveStage.FindNpc => "Find Lanternfish",
                ObjectiveStage.TalkStarfish => "Wake the first Thaw Anchor",
                ObjectiveStage.TalkSeaweed => "Wake all three Thaw Anchors",
                ObjectiveStage.CollectShards => "Collect Frost Crystals",
                ObjectiveStage.DefeatBoss => "Calm the Frostshell Guardian with Ice",
                ObjectiveStage.ExitLevel => "Cross the restored nursery gate",
                _ => "Frostbound Trench is flowing again",
            };
        if (CurrentChapter == 3)
            return stage switch
            {
                ObjectiveStage.FindNpc => "Find the Lost Shoal",
                ObjectiveStage.TalkStarfish => "Connect the first Tide Relay",
                ObjectiveStage.TalkSeaweed => "Reconnect all three Tide Relays",
                ObjectiveStage.CollectShards => "Collect Circuit Sparks",
                ObjectiveStage.DefeatBoss => "Restore the Overheated Core with Electric",
                ObjectiveStage.ExitLevel => "Light the Silent Lighthouse",
                _ => "Safe lights have returned to the ocean",
            };
        return GameStrings.LevelOneObjectiveLabel(stage);
    }
}

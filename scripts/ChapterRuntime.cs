using Godot;

namespace ShallowSeaDream;

/// Small presentation context shared by the chapter controller and the reusable HUD.
/// It is set by a level root in _EnterTree, before child HUD nodes enter _Ready.
public static class ChapterRuntime
{
    /// Temporarily hidden: the interior reef pillars that made each map a maze.
    /// Flip this back to true to restore them; the map outer bounds and the boss
    /// arena gate are deliberately NOT covered by this switch, because the level
    /// needs them to stay playable.
    public static readonly bool ReefMazeEnabled = false;

    public static int CurrentChapter { get; private set; } = 1;
    public static ElementForm RequiredForm { get; private set; } = ElementForm.Water;
    public static string FragmentLabel { get; private set; } = "Water Shards";
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
            BossName = "Chemical Waste Monster";
            Zones = new[] { "Frozen Inlet", "Ice Ridge", "Frost Nursery" };
        }
        else if (CurrentChapter == 3)
        {
            RequiredForm = ElementForm.Electric;
            FragmentLabel = "Circuit Sparks";
            BossName = "Oil Monster";
            Zones = new[] { "Broken Power Grid", "Warm Current", "Silent Lighthouse" };
        }
        else
        {
            RequiredForm = ElementForm.Water;
            FragmentLabel = "Water Shards";
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
                ObjectiveStage.TalkStarfish => "Wake the first Flow Switch",
                ObjectiveStage.TalkSeaweed => "Wake all three Flow Switches",
                ObjectiveStage.CollectShards => "Collect Frost Crystals",
                ObjectiveStage.DefeatBoss => "Freeze and clean up the Chemical Waste Monster",
                ObjectiveStage.ExitLevel => "Cross the restored nursery gate",
                _ => "Frozen Trench is flowing again",
            };
        if (CurrentChapter == 3)
            return stage switch
            {
                ObjectiveStage.FindNpc => "Find the Lost Shoal",
                ObjectiveStage.TalkStarfish => "Connect the first Power Relay",
                ObjectiveStage.TalkSeaweed => "Reconnect all three Power Relays",
                ObjectiveStage.CollectShards => "Collect Circuit Sparks",
                ObjectiveStage.DefeatBoss => "Clean up the Oil Monster with Electric",
                ObjectiveStage.ExitLevel => "Light the Silent Lighthouse",
                _ => "Safe lights have returned to the ocean",
            };
        return GameStrings.LevelOneObjectiveLabel(stage);
    }
}

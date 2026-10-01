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
                ObjectiveStage.FindNpc => "Find Lanternfish: learn why the water stopped",
                ObjectiveStage.TalkStarfish => "Start the first Flow Switch to move oxygen",
                ObjectiveStage.TalkSeaweed => "Restore all Flow Switches so the nursery can breathe",
                ObjectiveStage.CollectShards => "Collect Frost Crystals to contain the leaks",
                ObjectiveStage.DefeatBoss => "Use Ice from a distance to clean chemical waste",
                ObjectiveStage.ExitLevel => "Follow the clean current toward the lighthouse",
                _ => "Frozen Trench is flowing again",
            };
        if (CurrentChapter == 3)
            return stage switch
            {
                ObjectiveStage.FindNpc => "Find the Lost Shoal: learn why the reef went dark",
                ObjectiveStage.TalkStarfish => "Connect the first relay safely",
                ObjectiveStage.TalkSeaweed => "Reconnect all relays to restart the lighthouse",
                ObjectiveStage.CollectShards => "Collect Circuit Sparks for the cleanup system",
                ObjectiveStage.DefeatBoss => "Use Electric from a distance to pull oil away",
                ObjectiveStage.ExitLevel => "Light the Silent Lighthouse",
                _ => "Safe lights have returned to the ocean",
            };
        return GameStrings.LevelOneObjectiveLabel(stage);
    }
}

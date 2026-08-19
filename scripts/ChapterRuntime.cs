using Godot;

namespace ShallowSeaDream;

/// Small presentation context shared by the chapter controller and the reusable HUD.
/// It is set by a level root in _EnterTree, before child HUD nodes enter _Ready.
public static class ChapterRuntime
{
    public static int CurrentChapter { get; private set; } = 1;
    public static ElementForm RequiredForm { get; private set; } = ElementForm.Water;
    public static string FragmentLabel { get; private set; } = "潮汐碎片";
    public static string BossName { get; private set; } = GameConstants.MonsterBossName;
    public static string[] Zones { get; private set; } =
        { NarrativeData.ZoneShallows, NarrativeData.ZoneSediment, NarrativeData.ZoneDepths };

    public static void SetChapter(int chapter)
    {
        CurrentChapter = Mathf.Clamp(chapter, 1, 3);
        if (CurrentChapter == 2)
        {
            RequiredForm = ElementForm.Ice;
            FragmentLabel = "霜潮冰晶";
            BossName = "霜壳守望者";
            Zones = new[] { "冻潮入口", "冰脊回廊", "霜心育场" };
        }
        else if (CurrentChapter == 3)
        {
            RequiredForm = ElementForm.Electric;
            FragmentLabel = "回路电火花";
            BossName = "废热炉心";
            Zones = new[] { "沉船电网", "废热管廊", "断流灯塔" };
        }
        else
        {
            RequiredForm = ElementForm.Water;
            FragmentLabel = "潮汐碎片";
            BossName = GameConstants.MonsterBossName;
            Zones = new[] { NarrativeData.ZoneShallows, NarrativeData.ZoneSediment, NarrativeData.ZoneDepths };
        }
    }

    public static string ObjectiveLabel(ObjectiveStage stage)
    {
        if (CurrentChapter == 2)
            return stage switch
            {
                ObjectiveStage.FindNpc => "找到灯笼鱼·盏",
                ObjectiveStage.TalkStarfish => "唤醒第一座解冻锚",
                ObjectiveStage.TalkSeaweed => "让三座解冻锚重新流动",
                ObjectiveStage.CollectShards => "收集霜潮冰晶",
                ObjectiveStage.DefeatBoss => "用冰元素安抚霜壳守望者",
                ObjectiveStage.ExitLevel => "穿过复苏的霜心潮门",
                _ => "霜骨海沟恢复流动",
            };
        if (CurrentChapter == 3)
            return stage switch
            {
                ObjectiveStage.FindNpc => "找到迷途鱼群",
                ObjectiveStage.TalkStarfish => "接通第一座潮汐继电器",
                ObjectiveStage.TalkSeaweed => "重连三座海底回路",
                ObjectiveStage.CollectShards => "收集回路电火花",
                ObjectiveStage.DefeatBoss => "用电元素唤醒废热炉心",
                ObjectiveStage.ExitLevel => "点亮断流灯塔",
                _ => "海底灯火重新连成星河",
            };
        return GameStrings.LevelOneObjectiveLabel(stage);
    }
}

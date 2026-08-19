using System.Collections.Generic;

namespace ShallowSeaDream;

/// res://scripts/NarrativeData.cs
/// The expanded story content as clean DATA (DESIGN_BRIEF §1/§6/§7), kept separate
/// from gameplay logic. Holds: extra dialogue timelines (opening, multi-stage 岚婆婆,
/// new in-canon NPCs, boss pre/mid/post, ending), the 「微光潮汐记忆」 shard vignettes
/// (item 3), examinable 「残片笔记」 lore notes (item 5) and monster/creature codex
/// entries. All zh primary, anchored on the original canon (潮湾苗圃 nursery, 微光
/// jellyfish, water element, boss 潮涡巢母, NPCs 岚婆婆/海星/海草/汽油桶, tidal key).

/// A collectible 「微光潮汐记忆」 vignette — 1-3 short lines shown via the dialogue box.
public sealed record MemoryVignette(string Id, string Title, IReadOnlyList<string> Lines, string Zone);

/// An examinable 「残片笔记」 / lore object the player reads with E.
public sealed record LoreNote(string Id, string Title, IReadOnlyList<string> Lines, string Zone);

/// A monster / creature codex entry unlocked by proximity.
public sealed record CodexEntry(string Id, string Name, string Body, string Weakness);

public static class NarrativeData
{
    // --- Zone ids (item 7) ---
    public const string ZoneShallows = "浅滩";       // 浅滩 — opening / tutorial-adjacent
    public const string ZoneSediment = "沉积带";     // 沉积带 — middle, lore-dense
    public const string ZoneDepths = "巢母深处";     // 巢母深处 — boss approach

    // --- Extra timeline ids (string keys = log keys) ---
    public const string Opening = "opening";
    public const string GrannyMid = "granny_mid";       // during objectives
    public const string GrannyAfter = "granny_after";   // after boss
    public const string StarfishDeep = "starfish_deep"; // optional follow-up
    public const string SeaweedDeep = "seaweed_deep";   // optional follow-up
    public const string Hermit = "hermit";              // NEW NPC 寄居蟹老郑
    public const string Lantern = "lantern";            // NEW NPC 灯笼鱼·盏
    public const string Shoal = "shoal";                // NEW NPC 迷途鱼群 (optional)
    public const string BossPre = "boss_pre";
    public const string BossMid = "boss_mid";
    public const string Ending = "ending";
    public const string Chapter2Opening = "chapter2_opening";
    public const string Chapter2Guardian = "chapter2_guardian";
    public const string Chapter2Ending = "chapter2_ending";
    public const string Chapter3Opening = "chapter3_opening";
    public const string Chapter3Guardian = "chapter3_guardian";
    public const string Chapter3Ending = "chapter3_ending";

    private static readonly Dictionary<string, DialogueTimeline> Timelines = BuildTimelines();

    public static DialogueTimeline? GetTimeline(string id) =>
        Timelines.TryGetValue(id, out var t) ? t : null;

    public static IEnumerable<string> TimelineIds() => Timelines.Keys;

    private static Dictionary<string, DialogueTimeline> BuildTimelines()
    {
        const string G = DialogueData.SpeakerGranny;
        const string S = DialogueData.SpeakerShimmer;
        const string St = DialogueData.SpeakerStarfish;
        const string Sw = DialogueData.SpeakerSeaweed;
        const string H = DialogueData.SpeakerHermit;
        const string L = DialogueData.SpeakerLantern;
        const string Sh = DialogueData.SpeakerShoal;
        const string B = DialogueData.SpeakerBoss;

        var d = new Dictionary<string, DialogueTimeline>();

        // --- Richer opening (plays on level start) ---
        d[Opening] = new DialogueTimeline(Opening, new List<DialogueLine>
        {
            new(S, "潮湾苗圃……我出生的地方，曾经满是会发光的同伴。"),
            new(S, "现在水是浑的，光一盏一盏地熄了。"),
            new(S, "可我身上还亮着。也许，这点微光还能唤回些什么。"),
        });

        // --- 岚婆婆 multi-stage: mid (during collecting) ---
        d[GrannyMid] = new DialogueTimeline(GrannyMid, new List<DialogueLine>
        {
            new(G, "碎片在你掌心亮起来了，看见了吗？", "npc1giving"),
            new(G, "每一片，都是一段被污染冲散的潮汐记忆。", "npc1giving"),
            new(G, "收齐它们，你就听得懂巢母为何哭。", "npc1giving"),
        });

        // --- 岚婆婆 after boss ---
        d[GrannyAfter] = new DialogueTimeline(GrannyAfter, new List<DialogueLine>
        {
            new(G, "你没有杀死她。你把缠在她身上的脏东西，一点点解开了。", "npc1giving"),
            new(G, "这才是净化。记住这条路——它通向更深更冷的海。", "npc1giving"),
            new(S, "我会带着潮汐钥匙，去下一片海。", "npc1giving"),
        });

        // --- Deepened 海星 (optional follow-up) ---
        d[StarfishDeep] = new DialogueTimeline(StarfishDeep, new List<DialogueLine>
        {
            new(St, "你回来了。我数着你唤醒的光，一盏、两盏……", "starfish"),
            new(St, "我的兄妹被冲到了沉积带，再没回来。", "starfish"),
            new(St, "若你去那边，替我看看……他们是不是还亮着。", "starfish"),
        });

        // --- Deepened 海草 (optional follow-up) ---
        d[SeaweedDeep] = new DialogueTimeline(SeaweedDeep, new List<DialogueLine>
        {
            new(Sw, "巢母的呼吸又重了一分。她不是恶意，她是疼。", "seaweed"),
            new(Sw, "废液让她分不清自己和别人，于是把谁都缠进来。", "seaweed"),
            new(Sw, "用水，轻一点。净化不是打碎，是松开。", "seaweed"),
        });

        // --- NEW NPC: 寄居蟹老郑 (沉积带) — practical, weary, comic-melancholy ---
        d[Hermit] = new DialogueTimeline(Hermit, new List<DialogueLine>
        {
            new(H, "小光仔，别踩我的壳——哦不，这只是个易拉罐。", "hermit"),
            new(H, "我换了三个『家』了，全是人类丢下的硬壳。", "hermit"),
            new(H, "这片沉积带底下，埋着说不完的旧事。", "hermit", new List<DialogueChoice>
            {
                new("帮我指条路。", SetFlag: "hermit_route", GotoLabel: "hermit_route"),
                new("你还好吗，老郑？", SetFlag: "hermit_care", GotoLabel: "hermit_care"),
            }),
            new(H, "好不好的……活着就还得搬家。你这孩子，心是软的。", "hermit", Label: "hermit_care"),
            new(H, "往深处走，膜最薄的地方就是巢母。别硬碰，用水。", "hermit", Label: "hermit_route"),
        });

        // --- NEW NPC: 灯笼鱼·盏 (巢母深处) — a dimming light, hopeful ---
        d[Lantern] = new DialogueTimeline(Lantern, new List<DialogueLine>
        {
            new(L, "我这盏灯，是这片深处最后的亮了。", "lantern"),
            new(L, "油糊住了我的灯囊，可我不敢灭——灭了，就再没人认得回家的路。", "lantern"),
            new(L, "你身上的微光好暖。替我把路照下去，好吗？", "lantern"),
            new(S, "我会的。你也别灭。", "lantern"),
        });

        // --- NEW NPC: 迷途鱼群 (optional, 沉积带) — chorus voice ---
        d[Shoal] = new DialogueTimeline(Shoal, new List<DialogueLine>
        {
            new(Sh, "（许多细小的声音叠在一起）……出口在哪？出口在哪？", "shoal"),
            new(Sh, "我们跟着浊流游，越游越深，再也找不到上面的光。", "shoal"),
            new(Sh, "你发光……我们能跟着你吗？就一会儿也好。", "shoal"),
        });

        // --- Boss pre / mid (post = existing BossDefeated + Ending) ---
        d[BossPre] = new DialogueTimeline(BossPre, new List<DialogueLine>
        {
            new(B, "（低频的呜咽）……痛……谁……把我……缠住了……", "boss"),
            new(S, "她在求救。海草说得对——她不是敌人。"),
            new(S, "靠近她最薄的膜，用水，轻轻松开。"),
        });

        d[BossMid] = new DialogueTimeline(BossMid, new List<DialogueLine>
        {
            new(B, "（污浊从裂口里流走）……凉的……是水……", "boss"),
            new(S, "再坚持一下，被你缠进来的孩子们，正一个个浮回水面。"),
        });

        // --- Ending beat: hand over the tidal key, tease next sea ---
        d[Ending] = new DialogueTimeline(Ending, new List<DialogueLine>
        {
            new(G, "潮汐钥匙认了你。它会带你去冰封的『霜骨海沟』。", "npc1giving"),
            new(G, "那里的污染是冷的，水救不了，要用冰。", "npc1giving"),
            new(S, "潮湾苗圃的光，我留一半在这儿，照着回家的路。"),
            new(S, "另一半，我带去下一片海。——第二章，待续。"),
        });

        d[Chapter2Opening] = new DialogueTimeline(Chapter2Opening, new List<DialogueLine>
        {
            new(L, "微光！潮水在这里冻住了，鱼卵和海草都困在冰层下面。", "lantern"),
            new(S, "冰不是敌人。失去流动的海，才会让寒冷变成牢笼。"),
            new(L, "沿着冰脊唤醒三座解冻锚，让潮水重新找到路。", "lantern"),
        });
        d[Chapter2Guardian] = new DialogueTimeline(Chapter2Guardian, new List<DialogueLine>
        {
            new(B, "别靠近……我只能继续结冰，才能把污水挡在外面……", "boss"),
            new(S, "你守得太久了。让我用冰的共鸣，替你松开外壳。"),
        });
        d[Chapter2Ending] = new DialogueTimeline(Chapter2Ending, new List<DialogueLine>
        {
            new(B, "潮水……又在动了。原来守护，不一定要把一切冻住。", "boss"),
            new(L, "霜骨海沟亮起来了！前面却还有一片完全断电的海。", "lantern"),
            new(S, "那就把这片冰里的光，带去下一段回路。"),
        });
        d[Chapter3Opening] = new DialogueTimeline(Chapter3Opening, new List<DialogueLine>
        {
            new(Sh, "光断了……我们看不见同伴，也找不到产卵的礁石。", "shoal"),
            new(S, "这些旧电缆不该继续漏电，但安全的潮汐回路可以给生命指路。"),
            new(Sh, "请接通三座继电器。让灯塔只照亮海，不再灼伤海。", "shoal"),
        });
        d[Chapter3Guardian] = new DialogueTimeline(Chapter3Guardian, new List<DialogueLine>
        {
            new(B, "热……停不下来……烟把回路和海都堵住了……", "boss"),
            new(S, "我不会摧毁你。我会把失控的电导回灯塔，让你安静下来。"),
        });
        d[Chapter3Ending] = new DialogueTimeline(Chapter3Ending, new List<DialogueLine>
        {
            new(B, "炉火降下来了。余温可以孵化珊瑚，不必再烧黑海水。", "boss"),
            new(Sh, "看——每一盏灯都只照需要的地方，鱼群重新看见彼此了。", "shoal"),
            new(S, "保护海洋，不是替海做一次英雄；是让每一次选择，都不再把代价沉到海底。"),
            new(S, "只要还有人愿意修复，微光就会一盏一盏传下去。"),
        });

        return d;
    }

    // --- Memory vignettes (item 3): one per shard "memory" collected ---
    public static readonly IReadOnlyList<MemoryVignette> Memories = new List<MemoryVignette>
    {
        new("mem_1", "潮汐记忆·初亮", new[] { "一群幼小的微光第一次睁眼，", "整片苗圃像被点亮的星海。" }, ZoneShallows),
        new("mem_2", "潮汐记忆·暖流", new[] { "暖流穿过珊瑚，", "孩子们追着光影，笑出细小的气泡。" }, ZoneShallows),
        new("mem_3", "潮汐记忆·第一只桶", new[] { "某天，一只铁桶沉了下来，", "水面上的影子，第一次盖住了光。" }, ZoneShallows),
        new("mem_4", "潮汐记忆·浊流", new[] { "浊流来时，", "灯一盏盏地熄，谁也没来得及说再见。" }, ZoneSediment),
        new("mem_5", "潮汐记忆·沉积", new[] { "被冲散的同伴落进沉积带，", "光被泥沙一层层盖住，像睡着了。" }, ZoneSediment),
        new("mem_6", "潮汐记忆·岚婆婆的歌", new[] { "岚婆婆抱着最后几枚卵，", "用很轻的调子，把潮汐唱给它们听。" }, ZoneSediment),
        new("mem_7", "潮汐记忆·缠绕", new[] { "废液让小水母们彼此粘连，", "分不清你我，痛也连成一片。" }, ZoneDepths),
        new("mem_8", "潮汐记忆·呼吸", new[] { "那低频的呼吸，", "原来是无数被困的声音，在一起求救。" }, ZoneDepths),
        new("mem_9", "潮汐记忆·松开", new[] { "水流过缝隙，", "第一缕被缠住的光，挣脱了出来。" }, ZoneDepths),
        new("mem_10", "潮汐记忆·钥匙", new[] { "当最后一缕光归位，", "潮水深处，浮起一枚冷冷发亮的钥匙。" }, ZoneDepths),
    };

    public static MemoryVignette MemoryAt(int index) => Memories[index % Memories.Count];
    public static int MemoryCount => Memories.Count;

    public static MemoryVignette? FindMemory(string id)
    {
        foreach (var m in Memories) if (m.Id == id) return m;
        return null;
    }

    public static LoreNote? FindNote(string id)
    {
        foreach (var n in LoreNotes) if (n.Id == id) return n;
        return null;
    }

    public static CodexEntry? FindCodex(string id)
    {
        foreach (var c in CodexEntries) if (c.Id == id) return c;
        return null;
    }

    // --- Lore notes (item 5): examinable 残片笔记 / objects scattered across zones ---
    public static readonly IReadOnlyList<LoreNote> LoreNotes = new List<LoreNote>
    {
        new("lore_barrel", "残片·汽油桶", new[] { "桶身印着褪色的厂标：", "「装填后请妥善回收」——没人照做。" }, ZoneShallows),
        new("lore_net", "残片·断网", new[] { "一截被丢弃的渔网，", "缠着早已风干的、谁的鳞片。" }, ZoneShallows),
        new("lore_bottle", "残片·漂流瓶", new[] { "瓶里有张人类小孩的画：", "蓝色的海，和一条会发光的鱼。" }, ZoneShallows),
        new("lore_pipe", "残片·排污口", new[] { "锈蚀的管口仍在渗着浊液，", "苗圃的水，就是从这里开始变浑的。" }, ZoneSediment),
        new("lore_shell", "残片·空壳堆", new[] { "层层叠叠的空贝壳，", "老郑说，这是『搬不动的旧家』。" }, ZoneSediment),
        new("lore_log", "残片·观测日志", new[] { "一块刻字的石板：", "「浊流第七日，发光体数量减半。」" }, ZoneSediment),
        new("lore_membrane", "残片·薄膜", new[] { "一片巢母蜕落的膜，", "凑近能听见极轻的、像哭的声音。" }, ZoneDepths),
        new("lore_lantern", "残片·熄灯", new[] { "一盏沉底的灯笼鱼遗物，", "灯囊里凝固的油，黑得发亮。" }, ZoneDepths),
    };

    public static int LoreCount => LoreNotes.Count;

    // --- Monster / creature codex entries (item 5 log) ---
    public static readonly IReadOnlyList<CodexEntry> CodexEntries = new List<CodexEntry>
    {
        new("codex_boss", "潮涡巢母",
            "由无数小水母被废液浸泡粘连而成。它并非凶兽，而是一团连在一起的疼痛与求救。",
            "水（净化，非击杀）"),
        new("codex_invader_plastic", "塑料漂物",
            "随浊流漂荡的塑料污染体，会黯淡周围的光。", "水"),
        new("codex_invader_oil", "油渍残影",
            "油膜凝成的污染体，糊住灯囊与呼吸。", "水"),
        new("codex_invader_foam", "化学泡沫",
            "废液发酵的苍白泡沫，触之刺痛。", "水"),
    };
}

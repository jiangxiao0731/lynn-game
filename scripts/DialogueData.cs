using System;
using System.Collections.Generic;

namespace ShallowSeaDream;

/// res://scripts/DialogueData.cs
/// In-code dialogue "data files" (DESIGN_BRIEF §1, §7). zh text kept verbatim from
/// the brief as anchor lore, with a greatly expanded narrative arc built around it
/// (see NarrativeData for the new vignettes / lore / codex content).
///
/// A line carries an optional speaker portrait id (res://assets/img/npc_{id}.png via
/// AssetLoader) and an optional set of branching CHOICES. Picking a choice can set a
/// story flag and/or jump to a labelled line, enabling branching timelines.

/// One branch button: label shown to player; optional flag to set; optional jump target.
public sealed record DialogueChoice(string Text, string? SetFlag = null, string? GotoLabel = null);

/// One dialogue line. PortraitId selects the speaker art; Label allows choice jumps.
public sealed record DialogueLine(
    string Speaker,
    string Text,
    string? PortraitId = null,
    IReadOnlyList<DialogueChoice>? Choices = null,
    string? Label = null);

public sealed record DialogueTimeline(string Id, IReadOnlyList<DialogueLine> Lines);

public static class DialogueData
{
    // --- Core canon timeline ids (kept stable for save / log keys) ---
    public const string GrannyLan = "npc1giving";
    public const string Starfish = "starfish";
    public const string Seaweed = "seaweed";
    public const string JellyfishBox = "jellyfish1Giving";
    public const string BossDefeated = "boss_defeated";

    // Speaker display names (zh primary).
    public const string SpeakerGranny = "岚婆婆";
    public const string SpeakerStarfish = "海星";
    public const string SpeakerSeaweed = "海草";
    public const string SpeakerShimmer = "微光";
    public const string SpeakerBox = "汽油桶";
    public const string SpeakerHermit = "寄居蟹老郑";
    public const string SpeakerLantern = "灯笼鱼·盏";
    public const string SpeakerShoal = "迷途鱼群";
    public const string SpeakerBoss = "潮涡巢母";

    private static readonly Dictionary<string, DialogueTimeline> Timelines = Build();

    public static DialogueTimeline? Get(string id)
    {
        if (Timelines.TryGetValue(id, out var t)) return t;
        return NarrativeData.GetTimeline(id);
    }

    /// All registered timeline ids (core + narrative), for tooling / log seeding.
    public static IEnumerable<string> AllIds()
    {
        foreach (var k in Timelines.Keys) yield return k;
        foreach (var k in NarrativeData.TimelineIds()) yield return k;
    }

    private static Dictionary<string, DialogueTimeline> Build()
    {
        var d = new Dictionary<string, DialogueTimeline>();

        // 岚婆婆 — first meeting (anchor lore verbatim, with a small branching choice).
        d[GrannyLan] = new DialogueTimeline(GrannyLan, new List<DialogueLine>
        {
            new(SpeakerGranny, "孩子，你身上还留着没被污染的微光。", "npc1giving"),
            new(SpeakerGranny, "不同的污染，需要不同的净化之力。", "npc1giving"),
            new(SpeakerGranny, "第一章，我教你唤醒「水」的力量。", "npc1giving"),
            new(SpeakerGranny, "你愿意承接这份潮汐的托付吗？", "npc1giving", new List<DialogueChoice>
            {
                new("我愿意试试。", SetFlag: "granny_accept", GotoLabel: "granny_yes"),
                new("我还有些害怕……", SetFlag: "granny_hesitate", GotoLabel: "granny_soft"),
            }),
            new(SpeakerGranny, "害怕是对的。可海不会等怕的人。我陪你走第一段。", "npc1giving", Label: "granny_soft"),
            new(SpeakerGranny, "吸收附近的水碎片，靠近巢母时按 1 释放水元素。", "npc1giving", Label: "granny_yes"),
            new(SpeakerGranny, "更深的海里还有冰与电……但那是后话了。", "npc1giving"),
            new(SpeakerShimmer, "我会记住每一片被我唤回的光。", "npc1giving"),
        });

        d[Starfish] = new DialogueTimeline(Starfish, new List<DialogueLine>
        {
            new(SpeakerStarfish, "好久没有还会发光的同伴经过了。", "starfish"),
            new(SpeakerStarfish, "塑料让我的族人黯淡下去……", "starfish"),
            new(SpeakerStarfish, "深处有更暗的东西在脉动。", "starfish"),
        });

        d[Seaweed] = new DialogueTimeline(Seaweed, new List<DialogueLine>
        {
            new(SpeakerSeaweed, "你听见了吗？那低频的「呼吸」。", "seaweed"),
            new(SpeakerSeaweed, "它原本只是一群小水母，被废液浸泡得彼此粘连。", "seaweed"),
            new(SpeakerSeaweed, "先收集碎片，再用水元素击打它最薄的膜。", "seaweed"),
        });

        d[JellyfishBox] = new DialogueTimeline(JellyfishBox, new List<DialogueLine>
        {
            new(SpeakerShimmer, "这是什么……一只生锈的桶？"),
            new(SpeakerBox, "我原本是人类工厂里装汽油的容器……", "barrel"),
            new(SpeakerBox, "他们说，把我直接扔进海里最省事。", "barrel"),
            new(SpeakerBox, "我不想再漏了。求你，把我身上的油……带走。", "barrel"),
            new(SpeakerShimmer, "我把它记进了潮汐记忆里。它也曾想做点别的。"),
        });

        d[BossDefeated] = new DialogueTimeline(BossDefeated, new List<DialogueLine>
        {
            new(SpeakerShimmer, "巢母的外壳裂开了……"),
            new(SpeakerShimmer, "被困的微光，回到了水里。"),
            new(SpeakerShimmer, "一枚新的元素之球浮起——通往下一片海域的潮汐钥匙。"),
        });

        return d;
    }
}

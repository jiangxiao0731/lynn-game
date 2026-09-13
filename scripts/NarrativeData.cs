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
    public const string ZoneShallows = "The Shallows";
    public const string ZoneSediment = "Silt Passage";
    public const string ZoneDepths = "Brood Depths";

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
        const string F = DialogueData.SpeakerFrostshell;
        const string C = DialogueData.SpeakerCore;

        var d = new Dictionary<string, DialogueTimeline>();

        // --- Richer opening (plays on level start) ---
        d[Opening] = new DialogueTimeline(Opening, new List<DialogueLine>
        {
            new(S, "Tidepool Nursery. I was born here, when every corner still glowed."),
            new(S, "Now the water is cloudy, and one light after another has gone out."),
            new(S, "Most of this pollution came from land, carried here through drains and rivers."),
            new(S, "Mine is still here. Maybe it is enough to start bringing them back."),
        });

        // --- 岚婆婆 multi-stage: mid (during collecting) ---
        d[GrannyMid] = new DialogueTimeline(GrannyMid, new List<DialogueLine>
        {
            new(G, "The shards are glowing in your hands. See?", "npc1giving"),
            new(G, "Each one holds a memory scattered by the pollution.", "npc1giving"),
            new(G, "Bring them together, and you will understand why the Brood Mother is crying.", "npc1giving"),
        });

        // --- 岚婆婆 after boss ---
        d[GrannyAfter] = new DialogueTimeline(GrannyAfter, new List<DialogueLine>
        {
            new(G, "You treated her as a life to protect. You loosened the waste that trapped her.", "npc1giving"),
            new(G, "That is what restoration means. Remember it as you enter colder water.", "npc1giving"),
            new(S, "I will carry the Tidal Key into the next sea.", "npc1giving"),
        });

        // --- Deepened 海星 (optional follow-up) ---
        d[StarfishDeep] = new DialogueTimeline(StarfishDeep, new List<DialogueLine>
        {
            new(St, "You're back. I've been counting the lights you restored.", "starfish"),
            new(St, "My family was swept into the Silt Passage. They never came back.", "starfish"),
            new(St, "If you go there, please look for them. See if they still glow.", "starfish"),
        });

        // --- Deepened 海草 (optional follow-up) ---
        d[SeaweedDeep] = new DialogueTimeline(SeaweedDeep, new List<DialogueLine>
        {
            new(Sw, "The Brood Mother's pulse is getting heavier. She is not angry. She is in pain.", "seaweed"),
            new(Sw, "Wastewater blurred the line between her body and everyone else's.", "seaweed"),
            new(Sw, "Use Water gently. Restoration is not about breaking things. It is about setting them free.", "seaweed"),
        });

        // --- NEW NPC: 寄居蟹老郑 (沉积带) — practical, weary, comic-melancholy ---
        d[Hermit] = new DialogueTimeline(Hermit, new List<DialogueLine>
        {
            new(H, "Watch the shell—actually, it is just an old can.", "hermit"),
            new(H, "I have moved three times. Every home was something humans threw away.", "hermit"),
            new(H, "The silt below us is full of stories nobody wanted to keep.", "hermit", new List<DialogueChoice>
            {
                new("Which way should I go?", SetFlag: "hermit_route", GotoLabel: "hermit_route"),
                new("Are you okay, Old Zheng?", SetFlag: "hermit_care", GotoLabel: "hermit_care"),
            }),
            new(H, "I'm still here, so I keep moving. Thanks for asking.", "hermit", Label: "hermit_care"),
            new(H, "Go deeper. The membrane is weakest near the Brood Mother. Use Water and do not force it.", "hermit", Label: "hermit_route"),
        });

        // --- NEW NPC: 灯笼鱼·盏 (巢母深处) — a dimming light, hopeful ---
        d[Lantern] = new DialogueTimeline(Lantern, new List<DialogueLine>
        {
            new(L, "My light is the last one still burning down here.", "lantern"),
            new(L, "Oil covers my light organ, but I cannot go dark. The others need it to find home.", "lantern"),
            new(L, "Your glow feels warm. Will you carry it farther?", "lantern"),
            new(S, "I will. Keep yours on too.", "lantern"),
        });

        // --- NEW NPC: 迷途鱼群 (optional, 沉积带) — chorus voice ---
        d[Shoal] = new DialogueTimeline(Shoal, new List<DialogueLine>
        {
            new(Sh, "(Many small voices) Where is the way out?", "shoal"),
            new(Sh, "We followed the dirty current too far down. Now we cannot find the surface.", "shoal"),
            new(Sh, "You glow. Can we follow you for a little while?", "shoal"),
        });

        // --- Boss pre / mid (post = existing BossDefeated + Ending) ---
        d[BossPre] = new DialogueTimeline(BossPre, new List<DialogueLine>
        {
            new(B, "(A low cry) It hurts... something has tied us together...", "boss"),
            new(S, "She is asking for help. Seaweed was right. She is a creature in pain."),
            new(S, "I need to reach the thin part of the membrane and release Water gently."),
        });

        d[BossMid] = new DialogueTimeline(BossMid, new List<DialogueLine>
        {
            new(B, "(Pollution drains through the crack) Cool... water...", "boss"),
            new(S, "Hold on. The young lights caught inside you are rising back to the surface."),
        });

        // --- Ending beat: hand over the tidal key, tease next sea ---
        d[Ending] = new DialogueTimeline(Ending, new List<DialogueLine>
        {
            new(G, "The Tidal Key chose you. It will lead you to Frostbound Trench.", "npc1giving"),
            new(G, "The pollution there is locked in ice. Water alone will not be enough.", "npc1giving"),
            new(S, "I will leave part of Tidepool Nursery's light here, so everyone can find home."),
            new(S, "I will carry the rest into the next sea."),
        });

        d[Chapter2Opening] = new DialogueTimeline(Chapter2Opening, new List<DialogueLine>
        {
            new(L, "Shimmer! The current has stopped. Eggs and sea grass are trapped under polluted ice.", "lantern"),
            new(S, "The cold is not the real threat. Runoff fed an algal bloom, and its decay used up the oxygen."),
            new(L, "Wake the three Thaw Anchors along the ridge. Give the current a path.", "lantern"),
        });
        d[Chapter2Guardian] = new DialogueTimeline(Chapter2Guardian, new List<DialogueLine>
        {
            new(F, "Stay back... I have to keep freezing, or the polluted water will get through...", "boss2"),
            new(S, "You have guarded this place for too long. Let my Ice current loosen the shell."),
        });
        d[Chapter2Ending] = new DialogueTimeline(Chapter2Ending, new List<DialogueLine>
        {
            new(F, "The tide is moving again. I thought protection meant freezing everything in place.", "boss2"),
            new(L, "Frostbound Trench is bright again. But the sea ahead has lost all power.", "lantern"),
            new(S, "Then I will carry this light into the next circuit."),
        });
        d[Chapter3Opening] = new DialogueTimeline(Chapter3Opening, new List<DialogueLine>
        {
            new(Sh, "The ocean carried the heat here. The coral turned pale, and the old grid failed.", "shoal"),
            new(S, "Greenhouse gases trap extra heat. The ocean absorbs most of it."),
            new(Sh, "Reconnect the three relays. Give the lighthouse clean, controlled power.", "shoal"),
        });
        d[Chapter3Guardian] = new DialogueTimeline(Chapter3Guardian, new List<DialogueLine>
        {
            new(C, "Too hot... I cannot shut down... smoke is blocking the circuit and the water...", "boss3"),
            new(S, "I will not destroy you. I will guide the loose current back to the lighthouse."),
        });
        d[Chapter3Ending] = new DialogueTimeline(Chapter3Ending, new List<DialogueLine>
        {
            new(C, "The furnace is cooling. Its warmth can help coral grow instead of burning the water.", "boss3"),
            new(Sh, "Look. Each light shines only where it is needed. The shoal can see itself again.", "shoal"),
            new(S, "Protecting the ocean is not one heroic act. It is choosing not to hide our costs underwater."),
            new(S, "As long as people keep repairing what was harmed, one light can lead to another."),
        });

        // Residents painted for each chapter register their own timelines, so new art
        // only has to be listed once, in ChapterCast.
        foreach (var entry in ChapterCast.Timelines()) d[entry.Key] = entry.Value;

        return d;
    }

    // --- Memory vignettes (item 3): one per shard "memory" collected ---
    public static readonly IReadOnlyList<MemoryVignette> Memories = new List<MemoryVignette>
    {
        new("mem_1", "Tidal Memory · First Light", new[] { "A group of young jellyfish opened their eyes.", "The whole nursery became a field of stars." }, ZoneShallows),
        new("mem_2", "Tidal Memory · Warm Current", new[] { "A warm current moved through the coral.", "Young lights chased it and left bubbles behind." }, ZoneShallows),
        new("mem_3", "Tidal Memory · The First Barrel", new[] { "One day, a metal barrel sank into the nursery.", "For the first time, a shadow from above covered the light." }, ZoneShallows),
        new("mem_4", "Tidal Memory · Dirty Current", new[] { "The dirty current arrived without warning.", "The lights went out one by one. No one had time to say goodbye." }, ZoneSediment),
        new("mem_5", "Tidal Memory · Buried Light", new[] { "The current scattered the young jellyfish into the silt.", "Layer after layer covered their light." }, ZoneSediment),
        new("mem_6", "Tidal Memory · Granny Lan's Song", new[] { "Granny Lan held the last few eggs.", "She sang the rhythm of the tide to them." }, ZoneSediment),
        new("mem_7", "Tidal Memory · Tangled", new[] { "Wastewater fused the young jellyfish together.", "Their bodies and their pain became hard to separate." }, ZoneDepths),
        new("mem_8", "Tidal Memory · Breath", new[] { "The low pulse was not a threat.", "It was many trapped voices asking for help." }, ZoneDepths),
        new("mem_9", "Tidal Memory · Release", new[] { "Water reached the gap in the membrane.", "The first trapped light pulled free." }, ZoneDepths),
        new("mem_10", "Tidal Memory · The Key", new[] { "When the last light returned to its place,", "a cold Tidal Key rose from the deep." }, ZoneDepths),
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
        new("lore_barrel", "Fragment · Oil Barrel", new[] { "A faded factory label is still visible:", "Recycle safely after use. No one did." }, ZoneShallows),
        new("lore_net", "Fragment · Abandoned Net", new[] { "A torn fishing net lies across the reef.", "Even without a fisher, lost gear can keep trapping animals for years." }, ZoneShallows),
        new("lore_bottle", "Fragment · Message Bottle", new[] { "Inside is a child's drawing:", "a blue ocean and a fish that gives off light." }, ZoneShallows),
        new("lore_pipe", "Fragment · Drain Pipe", new[] { "Runoff and wastewater still leak from the rusted pipe.", "The nursery became cloudy from pollution that began on land." }, ZoneSediment),
        new("lore_shell", "Fragment · Empty Shells", new[] { "Empty shells are stacked in the silt.", "Old Zheng calls them homes too heavy to move." }, ZoneSediment),
        new("lore_log", "Fragment · Survey Log", new[] { "Words are carved into a stone plate:", "Day seven of the dirty current. Half the lights are gone." }, ZoneSediment),
        new("lore_membrane", "Fragment · Membrane", new[] { "A thin piece of the Brood Mother's membrane.", "Up close, it sounds almost like crying." }, ZoneDepths),
        new("lore_lantern", "Fragment · Dark Lantern", new[] { "The remains of a lanternfish rest on the seabed.", "Oil has hardened inside its light organ." }, ZoneDepths),
    };

    public static int LoreCount => LoreNotes.Count;

    // --- Monster / creature codex entries (item 5 log) ---
    public static readonly IReadOnlyList<CodexEntry> CodexEntries = new List<CodexEntry>
    {
        new("codex_boss", "Brood Mother",
            "Wastewater fused many young jellyfish into one body. She is not a predator. She is many lives trapped in the same pain.",
            "Water — gentle restoration"),
        new("codex_invader_plastic", "Plastic Drifter",
            "A moving mass of plastic waste that dims nearby life.", "Water"),
        new("codex_invader_oil", "Oil Shadow",
            "A creature formed from an oil film. It blocks light and breathing.", "Water"),
        new("codex_invader_foam", "Chemical Foam",
            "Pale foam created by industrial waste. Contact causes pollution damage.", "Water"),
    };
}

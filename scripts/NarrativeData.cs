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
/// `Picture` is shown on the note's last line.
public sealed record LoreNote(string Id, string Title, IReadOnlyList<string> Lines, string Zone,
    DialoguePicture? Picture = null);

/// A monster / creature codex entry unlocked by proximity.
public sealed record CodexEntry(string Id, string Name, string Body, string Weakness);

public static class NarrativeData
{
    // --- Zone ids (item 7) ---
    public const string ZoneShallows = "The Shallows";
    public const string ZoneSediment = "Silt Passage";
    public const string ZoneDepths = "Tangled Depths";

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
            new(S, "This is where I was born. It used to be full of light."),
            new(S, "But now the water is cloudy, and the lights are disappearing one by one."),
            new(S, "A lot of this pollution came from land, carried here by drains and rivers."),
            new(S, "My light is still here. I can use it to clean what people left behind."),
            new(S, "First I need Granny Lan. She has watched over this bay longer than anyone.", null,
                Picture: new(AssetLoader.NpcPortrait("npc1giving"), "Granny Lan · she waits in the Shallows")),
        });

        // --- 岚婆婆 multi-stage: mid (during collecting) ---
        d[GrannyMid] = new DialogueTimeline(GrannyMid, new List<DialogueLine>
        {
            new(G, "Look—the plastic you cleaned is no longer choking the water.", "npc1giving"),
            new(G, "Each piece you process gives your light a little more strength.", "npc1giving"),
            new(G, "Clean enough of it, and you'll be ready to face the Plastic Monster.", "npc1giving"),
        });

        // --- 岚婆婆 after boss ---
        d[GrannyAfter] = new DialogueTimeline(GrannyAfter, new List<DialogueLine>
        {
            new(G, "You cleared away the plastic and freed the animals trapped inside.", "npc1giving"),
            new(G, "Up on land, some people do this work too. They haul lost nets out of the sea, tonne by tonne.", "npc1giving",
                Picture: DialoguePicture.Photo("ch1_cleanup.jpg",
                    "A clean-up crew lifts a tangle of derelict fishing nets on Midway Atoll.", "U.S. Fish and Wildlife Service")),
            new(G, "That's what restoring the ocean means. Remember that when you reach the colder water.", "npc1giving"),
            new(S, "I will. I'll follow the clean current to the next area.", "npc1giving"),
        });

        // --- Deepened 海星 (optional follow-up) ---
        d[StarfishDeep] = new DialogueTimeline(StarfishDeep, new List<DialogueLine>
        {
            new(St, "You're back! I've been counting the lights you brought back.", "starfish"),
            new(St, "My family was swept into the Dirty Channel. They still haven't returned.", "starfish"),
            new(St, "If you're heading that way, could you look for them for me?", "starfish"),
        });

        // --- Deepened 海草 (optional follow-up) ---
        d[SeaweedDeep] = new DialogueTimeline(SeaweedDeep, new List<DialogueLine>
        {
            new(Sw, "The Plastic Monster is getting bigger as more trash washes in.", "seaweed"),
            new(Sw, "It is made from plastic bottles, bags, and abandoned fishing gear.", "seaweed"),
            new(Sw, "We are not here just to hit it. Clean the small trash first, then free the animals trapped inside.", "seaweed"),
        });

        // --- NEW NPC: 寄居蟹老郑 (沉积带) — practical, weary, comic-melancholy ---
        d[Hermit] = new DialogueTimeline(Hermit, new List<DialogueLine>
        {
            new(H, "Watch out for my shell—or, actually, it's just an old tin can.", "hermit"),
            new(H, "I've moved house three times now. Every home I've lived in was something humans threw away.", "hermit"),
            new(H, "The silt down here is full of things people didn't need anymore—and stories, too.", "hermit", new List<DialogueChoice>
            {
                new("Which way should I go?", SetFlag: "hermit_route", GotoLabel: "hermit_route"),
                new("Are you okay, Old Zheng?", SetFlag: "hermit_care", GotoLabel: "hermit_care"),
            }),
            new(H, "I'm okay. I just have to keep moving and find a safer home.", "hermit", Label: "hermit_care"),
            new(H, "Head deeper into the bay. Clean the loose plastic first, then aim at the weakest part of the Plastic Monster.", "hermit", Label: "hermit_route"),
        });

        // --- NEW NPC: 灯笼鱼·盏 (巢母深处) — a dimming light, hopeful ---
        d[Lantern] = new DialogueTimeline(Lantern, new List<DialogueLine>
        {
            new(L, "Down here in the deep, mine is the only light still flickering.", "lantern"),
            new(L, "The oil keeps washing over, trying to smother my light, but I can't let it go out. My companions need my light to guide them home.", "lantern"),
            new(S, "What should I do first?", null, new List<DialogueChoice>
            {
                new("Help me find them.", SetFlag: "lantern_find", GotoLabel: "lantern_find"),
                new("I'll keep moving.", SetFlag: "lantern_ready", GotoLabel: "lantern_continue"),
            }),
            new(L, "Follow the dim lights. If one goes out, stop and look around. Someone may be stuck nearby.", "lantern", Label: "lantern_find"),
            new(L, "Your light feels warm. Could you help guide the others home?", "lantern", Label: "lantern_continue"),
            new(S, "I'll look for every light I can."),
            new(S, "I will. Just promise me you'll keep your light on too.", "lantern"),
        });

        // --- NEW NPC: 迷途鱼群 (optional, 沉积带) — chorus voice ---
        d[Shoal] = new DialogueTimeline(Shoal, new List<DialogueLine>
        {
            new(Sh, "(A chorus of tiny voices) Which way is the exit? We've lost our bearings.", "shoal"),
            new(Sh, "We followed that stream of dirty water too deep; now we can't see the way back.", "shoal"),
            new(S, "Should I lead you now?", null, new List<DialogueChoice>
            {
                new("Stay close to me.", SetFlag: "shoal_follow", GotoLabel: "shoal_follow"),
                new("Wait where it is safer.", SetFlag: "shoal_wait", GotoLabel: "shoal_continue"),
            }),
            new(Sh, "Okay. We will follow your light, but we will not rush ahead.", "shoal", Label: "shoal_follow"),
            new(Sh, "You're glowing... Could we follow you for a while?", "shoal", Label: "shoal_continue"),
        });

        // --- Boss combat notes. BossMid now plays after defeat, not mid-fight. ---
        d[BossPre] = new DialogueTimeline(BossPre, new List<DialogueLine>
        {
            new(B, "More trash... It keeps piling up...", "boss"),
            new(S, "This monster is made from all the plastic that collected here."),
            new(S, "How should I handle this?", null, new List<DialogueChoice>
            {
                new("Keep distance and aim.", SetFlag: "boss1_careful", GotoLabel: "boss1_careful"),
                new("Go in fast.", SetFlag: "boss1_rush", GotoLabel: "boss1_continue"),
            }),
            new(S, "I should not swim into it. I can help from outside the danger area.", Label: "boss1_careful"),
            new(S, "I need to use the cleanup power I saved from the plastic pieces.", Label: "boss1_continue"),
            new(S, "If I rush, I will just get caught too."),
        });

        d[BossMid] = new DialogueTimeline(BossMid, new List<DialogueLine>
        {
            new(B, "(Plastic bottles and bags break loose. Shimmer's light pulls them out of the water.)", "boss"),
            new(B, "I was never one creature. I was everything that got thrown away and left here.", "boss"),
            new(S, "Then I'll finish cleaning what people left here."),
            new(S, "The trapped animals are free now."),
        });

        // --- Ending beat: hand over the tidal key, tease next sea ---
        d[Ending] = new DialogueTimeline(Ending, new List<DialogueLine>
        {
            new(G, "The clean current is reacting to you. It should open the way to the Frozen Trench.", "npc1giving",
                Picture: new(AssetLoader.ChapterBackground(2, 0), "Next · Frozen Trench")),
            new(G, "The pollution there is different. Water alone will not be enough.", "npc1giving"),
            new(S, "I'll leave some of my light here so everyone can find their way home."),
            new(S, "I'll carry the rest with me."),
        });

        d[Chapter2Opening] = new DialogueTimeline(Chapter2Opening, new List<DialogueLine>
        {
            new(L, "Shimmer! The current has completely stopped. Fish eggs and seaweed are trapped in low-oxygen water.", "lantern"),
            new(S, "The cold isn't the main problem. Fertilizer and wastewater washed in from land. That fed a huge algae bloom."),
            new(S, "So the algae is not the thing we are saving?", null, new List<DialogueChoice>
            {
                new("Ask what caused the bloom.", SetFlag: "ch2_ask_source", GotoLabel: "ch2_source"),
                new("Focus on the switches.", SetFlag: "ch2_focus_switches", GotoLabel: "ch2_switches"),
            }),
            new(L, "Right. Too much algae is the symptom. When it dies and breaks down, it uses up oxygen, and animals can't breathe.", "lantern", Label: "ch2_source"),
            new(S, "It happens in real lakes and seas too. From space, a harmful bloom can look like green paint poured into the water.", null,
                Label: "ch2_switches",
                Picture: DialoguePicture.Photo("ch2_algal_bloom.jpg",
                    "An algal bloom spreading across Lake Erie, seen by satellite in September 2017.",
                    "NOAA Great Lakes Environmental Research Laboratory")),
            new(L, "Turn on the three Flow Switches along the ridge. Then clean the nutrient runoff patches so you have enough strength for the bloom.", "lantern",
                Picture: new(AssetLoader.MemoryIcon, "Flow Switch · three along the ridge")),
        });
        d[Chapter2Guardian] = new DialogueTimeline(Chapter2Guardian, new List<DialogueLine>
        {
            new(F, "(The bloom shrinks. Green clouds pull away from the eggs.)", "boss2"),
            new(F, "I grew because every runoff stream brought more nutrients to the same quiet water.", "boss2"),
            new(S, "Then the cleanup has to start upstream, before the nutrients feed another bloom."),
            new(S, "For now, the trench can breathe again."),
        });
        d[Chapter2Ending] = new DialogueTimeline(Chapter2Ending, new List<DialogueLine>
        {
            new(F, "(The dead-zone bloom breaks apart, and oxygen slowly returns to the trench.)", "boss2"),
            new(L, "The Frozen Trench is glowing again! But the waters ahead are covered in oil and heat.", "lantern",
                Picture: new(AssetLoader.ChapterBackground(3, 0), "Next · The Old Lighthouse")),
            new(S, "Then I'll bring this cleaned-up light to the next area."),
        });
        d[Chapter3Opening] = new DialogueTimeline(Chapter3Opening, new List<DialogueLine>
        {
            new(Sh, "The ocean absorbed too much heat. The coral turned pale, and then the old power grid failed.", "shoal"),
            new(S, "Greenhouse gases trap excess heat, and the ocean eventually absorbs most of it."),
            new(S, "What can I actually fix here?", null, new List<DialogueChoice>
            {
                new("Ask about the relays.", SetFlag: "ch3_ask_relays", GotoLabel: "ch3_relays"),
                new("Keep going.", SetFlag: "ch3_ready", GotoLabel: "ch3_continue"),
            }),
            new(Sh, "The relays can restart safe equipment. It will not fix everything, but it gives the reef a chance.", "shoal", Label: "ch3_relays"),
            new(Sh, "Reconnect the three relays. Then clean the oil patches so your electric pulse is strong enough for the monster.", "shoal",
                Label: "ch3_continue",
                Picture: new(AssetLoader.MemoryIcon, "Power Relay · three to reconnect")),
        });
        d[Chapter3Guardian] = new DialogueTimeline(Chapter3Guardian, new List<DialogueLine>
        {
            new(C, "(The oil pulls away from the reef and gathers into a dark coil.)", "boss3"),
            new(C, "I spread wherever the water carried me. I covered things that were still alive.", "boss3"),
            new(S, "Then I clean what is left, and people have to stop more oil from entering the water."),
            new(S, "The lighthouse can guide the reef again."),
        });
        d[Chapter3Ending] = new DialogueTimeline(Chapter3Ending, new List<DialogueLine>
        {
            new(C, "(The oil separates from the water. The monster collapses into waste that can finally be removed.)", "boss3"),
            new(Sh, "Look! The lights are working again.", "shoal"),
            new(S, "Protecting the ocean isn't one big heroic act. It means facing the damage instead of hiding it beneath the surface."),
            new(S, "Out there, people wash oil from seabirds one feather at a time. It is slow, and many of those birds fly again.", null,
                Picture: DialoguePicture.Photo("ch3_pelican_cleaned.jpg",
                    "A brown pelican after being cleaned at an oiled-bird rehabilitation centre in Alabama, 2010.",
                    "U.S. Fish and Wildlife Service")),
            new(S, "If people keep making better choices, one small change can lead to another."),
        });

        // Residents painted for each chapter register their own timelines, so new art
        // only has to be listed once, in ChapterCast.
        foreach (var entry in ChapterCast.Timelines()) d[entry.Key] = entry.Value;

        return d;
    }

    // --- Memory vignettes (item 3): one per shard "memory" collected ---
    public static readonly IReadOnlyList<MemoryVignette> Memories = new List<MemoryVignette>
    {
        new("mem_1", "Ocean Memory · First Light", new[] { "A swarm of young jellyfish opened their eyes.", "The entire nursery transformed into a sea of stars." }, ZoneShallows),
        new("mem_2", "Ocean Memory · Warm Current", new[] { "A warm current wove its way through the coral thickets.", "Young specks of light chased after it, leaving a trail of bubbles in their wake." }, ZoneShallows),
        new("mem_3", "Ocean Memory · The First Barrel", new[] { "One day, a metal barrel sank into the nursery.", "For the first time, a shadow from above blocked out the light." }, ZoneShallows),
        new("mem_4", "Ocean Memory · Filthy Current", new[] { "The stream of filthy water arrived without warning.", "The lights went out, one by one." }, ZoneSediment),
        new("mem_5", "Ocean Memory · Buried Light", new[] { "The current scattered the young sea creatures into the silt.", "Layer upon layer buried their light." }, ZoneSediment),
        new("mem_6", "Ocean Memory · Granny Lan's Song", new[] { "Granny Lan held the last few eggs.", "She sang the rhythm of the tides to them." }, ZoneSediment),
        new("mem_7", "Ocean Memory · Tangled Fate", new[] { "The wastewater fused the young creatures together.", "Their bodies and their pain became inseparable." }, ZoneDepths),
        new("mem_8", "Ocean Memory · Breathing", new[] { "A slow pulse is not a threat.", "Many trapped voices are crying out for help." }, ZoneDepths),
        new("mem_9", "Ocean Memory · Release", new[] { "The water rushed through the gaps in the trash pile.", "The first trapped light was set free." }, ZoneDepths),
        new("mem_10", "Ocean Memory · The Key", new[] { "When the last light returned to its place,", "a cold tidal key rose up." }, ZoneDepths),
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
        new("lore_barrel", "Fragment · Oil Barrel", new[] { "The faded factory label is still visible:", "\"Dispose of safely after use.\" Yet, no one ever did." }, ZoneShallows),
        new("lore_net", "Fragment · Abandoned Net", new[] { "A torn fishing net lies scattered across the reef.", "Even without a fisherman, lost gear can keep animals trapped for years." }, ZoneShallows,
            DialoguePicture.Photo("ch1_seal_net.jpg", "A Hawaiian monk seal caught in a derelict fishing net.", "NOAA Fisheries")),
        new("lore_bottle", "Fragment · Message Bottle", new[] { "Inside is a child's drawing:", "A blue ocean and glowing fish." }, ZoneShallows),
        new("lore_pipe", "Fragment · Drainage Pipe", new[] { "Runoff and wastewater still leak from the rusted pipe.", "Pollution from the land has made the nursery murky." }, ZoneSediment),
        new("lore_shell", "Fragment · Empty Shell", new[] { "Empty shells lie piled in the silt.", "Old Zheng says their homes have become too heavy; he can barely move them anymore." }, ZoneSediment),
        new("lore_log", "Fragment · Survey Log", new[] { "Text carved into a stone slab:", "Day seven of the dirty water flow. Half the lights have gone out." }, ZoneSediment),
        new("lore_plastic_sheet", "Fragment · Torn Plastic Sheet", new[] { "A torn sheet of plastic from the Plastic Monster.", "Small animals were trapped beneath it." }, ZoneDepths),
        new("lore_lantern", "Fragment · Dark Lantern", new[] { "The remains of an anglerfish lie on the seabed.", "The oil inside its glowing organ has hardened." }, ZoneDepths),
    };

    public static int LoreCount => LoreNotes.Count;

    // --- Monster / creature codex entries (item 5 log) ---
    public static readonly IReadOnlyList<CodexEntry> CodexEntries = new List<CodexEntry>
    {
        new("codex_boss", "Plastic Monster",
            "A giant monster formed from plastic bottles, bags, and abandoned fishing gear. It grows as more trash enters the bay.",
            "Water — mild restoration"),
        new("codex_invader_plastic", "Plastic Pile",
            "Masses of drifting plastic waste cast a gloom over life in the area. Sea animals are trapped inside the trash.", "Water — gentle restoration"),
        new("codex_invader_oil", "Oil Blob",
            "A creature formed from an oil film. It blocks light and breathing.", "Water"),
        new("codex_invader_foam", "Toxic Foam",
            "Pale foam created by industrial waste. Contact causes pollution damage.", "Water"),
    };
}

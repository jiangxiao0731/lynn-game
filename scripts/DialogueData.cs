using System;
using System.Collections.Generic;
using Godot;

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

/// A picture shown inside a line's bubble. Either the game's own art (who to find,
/// what to pick up, where you are going next) or a real photograph that shows the
/// harm the line is talking about. Photos always carry a credit; art never does.
public sealed record DialoguePicture(string Path, string Caption, string? Credit = null)
{
    public bool IsPhoto => Credit != null;

    /// A public-domain photo from res://assets/photos (see assets/photos/CREDITS.md).
    public static DialoguePicture Photo(string file, string caption, string credit) =>
        new($"res://assets/photos/{file}", caption, credit);

    public static DialoguePicture ResultBeforeForChapter(int chapter) => chapter switch
    {
        2 => Photo("ch2_toxic_algae_lake_erie.jpg",
            "A thick harmful algal bloom spreads across Lake Erie.",
            "NOAA"),
        3 => Photo("ch3_oil_spill_satellite.jpg",
            "The Deepwater Horizon oil slick spreads across the Gulf of Mexico.",
            "NASA"),
        _ => Photo("ch1_albatross_plastic_chick.jpg",
            "A young albatross sits beside plastic debris on Midway Atoll.",
            "U.S. Fish and Wildlife Service"),
    };

    public static DialoguePicture ResultAfterForChapter(int chapter) => chapter switch
    {
        2 => Photo("ch2_after_monitor_buoy.jpg",
            "A monitoring buoy tracks water quality and harmful bloom conditions in real time.",
            "USGS"),
        3 => Photo("ch3_after_coral_restoration.jpg",
            "NOAA divers prepare coral restoration work in habitat injured by the 2010 oil spill.",
            "NOAA Fisheries / Chris Gardner"),
        _ => Photo("ch1_after_beach_cleanup.jpg",
            "Volunteers clean shoreline near the Ahua Reef restored wetlands in Hawaiʻi.",
            "U.S. Navy / MC1 Daniel Barker"),
    };

    /// A rotating public-domain reality check for conversations that otherwise only
    /// carry character art. Every timeline now opens with real-world context, while
    /// authored line-specific photos still take priority.
    public static DialoguePicture PollutionForChapter(int chapter, int variant) => chapter switch
    {
        2 => Mathf.PosMod(variant, 7) switch
        {
            0 => Photo("ch2_algal_bloom.jpg", "A harmful algal bloom spreading across Lake Erie, seen from space.", "NOAA GLERL"),
            1 => Photo("ch2_sediment_plume.jpg", "Sediment and nutrients flowing from land into the Gulf of Mexico.", "NASA Goddard Space Flight Center"),
            2 => Photo("ch2_dead_zone.jpg", "A Gulf survey maps bottom water with too little oxygen for most marine life.", "NOAA"),
            3 => Photo("ch2_toxic_algae_lake_erie.jpg", "A thick green algal bloom spreads across Lake Erie.", "NOAA"),
            4 => Photo("ch2_lake_erie_habs_2015.jpg", "Harmful algal bloom water in Lake Erie during a 2015 survey.", "NOAA GLERL"),
            5 => Photo("ch2_habs_sample.jpg", "A water sample shows how dense a harmful algal bloom can become.", "NOAA GLERL"),
            _ => Photo("ch2_habs_buoy.jpg", "Scientists monitor harmful algal bloom conditions on Lake Erie.", "NOAA GLERL"),
        },
        3 => Mathf.PosMod(variant, 6) switch
        {
            0 => Photo("ch3_oiled_pelican.jpg", "A wildlife officer reaches an oiled brown pelican after the Deepwater Horizon spill.", "U.S. Fish and Wildlife Service"),
            1 => Photo("ch3_bleaching.jpg", "Bleached brain coral during the 2023 Florida Keys marine heatwave.", "NOAA"),
            2 => Photo("ch3_oil_slick.jpg", "Oil smoothing the surface of the Gulf of Mexico, seen by satellite.", "NASA Goddard Space Flight Center"),
            3 => Photo("ch3_rig_fire.jpg", "The Deepwater Horizon drilling rig burning in April 2010.", "U.S. Coast Guard"),
            4 => Photo("ch3_oil_spill_satellite.jpg", "The Deepwater Horizon oil slick spreading across the Gulf of Mexico.", "NASA"),
            _ => Photo("ch3_oil_approaches_coast.jpg", "Oil response teams watch slick water near the Gulf coast.", "U.S. Navy"),
        },
        _ => Mathf.PosMod(variant, 7) switch
        {
            0 => Photo("ch1_albatross_debris.jpg", "A Laysan albatross chick surrounded by marine debris on Midway Atoll.", "NOAA Marine Debris Program"),
            1 => Photo("ch1_seal_net.jpg", "A Hawaiian monk seal caught in an abandoned fishing net.", "NOAA Fisheries"),
            2 => Photo("ch1_cleanup.jpg", "A cleanup crew lifts derelict fishing gear from Midway Atoll.", "U.S. Fish and Wildlife Service"),
            3 => Photo("ch1_albatross_plastic_chick.jpg", "A young albatross sits beside plastic debris on Midway Atoll.", "U.S. Fish and Wildlife Service"),
            4 => Photo("ch1_derelict_gear_reef.jpg", "A NOAA diver removes derelict fishing gear from a reef.", "NOAA"),
            5 => Photo("ch1_albatross_cleanup.jpg", "Albatrosses stand near plastic debris collected on Midway Atoll.", "U.S. Fish and Wildlife Service"),
            _ => Photo("ch1_laysan_plastic_remains.jpg", "Plastic pieces remain around a young albatross on Midway Atoll.", "U.S. Fish and Wildlife Service"),
        },
    };
}

/// One dialogue line. PortraitId selects the speaker art; Label allows choice jumps.
public sealed record DialogueLine(
    string Speaker,
    string Text,
    string? PortraitId = null,
    IReadOnlyList<DialogueChoice>? Choices = null,
    string? Label = null,
    DialoguePicture? Picture = null);

public sealed record DialogueTimeline(string Id, IReadOnlyList<DialogueLine> Lines);

public static class DialogueData
{
    // --- Core canon timeline ids (kept stable for save / log keys) ---
    public const string GrannyLan = "npc1giving";
    public const string Starfish = "starfish";
    public const string Seaweed = "seaweed";
    public const string JellyfishBox = "jellyfish1Giving";
    public const string BossDefeated = "boss_defeated";

    // Speaker display names.
    public const string SpeakerGranny = "Granny Lan";
    public const string SpeakerStarfish = "Starfish";
    public const string SpeakerSeaweed = "Seaweed";
    public const string SpeakerShimmer = "Shimmer";
    public const string SpeakerBox = "Oil Drum";
    public const string SpeakerHermit = "Old Zheng";
    public const string SpeakerLantern = "Lanternfish";
    public const string SpeakerShoal = "Lost Shoal";
    public const string SpeakerBoss = "Plastic Monster";
    public const string SpeakerFrostshell = "Chemical Waste Monster";
    public const string SpeakerCore = "Oil Monster";

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
            new(SpeakerGranny, "Your light is still clear, Shimmer. That's a good sign.", "npc1giving"),
            new(SpeakerGranny, "You are not here to collect water. Your own light is the thing that cleans.", "npc1giving"),
            new(SpeakerGranny, "Start small. Clean the plastic bottles and loose trash first, and your light will grow steadier.", "npc1giving"),
            new(SpeakerGranny, "Will you help me clean up this bay?", "npc1giving", new List<DialogueChoice>
            {
                new("I'll give it a try.", SetFlag: "granny_accept", GotoLabel: "granny_yes"),
                new("I'm a little scared.", SetFlag: "granny_hesitate", GotoLabel: "granny_soft"),
            }),
            new(SpeakerGranny, "It's okay to be scared. I'll guide you through the first part.", "npc1giving", Label: "granny_soft"),
            new(SpeakerGranny, "Go clean the nearby plastic pieces. When the Plastic Monster attacks, stay back and press 1 to push that cleaned-up power into it.", "npc1giving", Label: "granny_yes",
                Picture: new(AssetLoader.ChapterElement(1), "Clean these · 8 pieces before the monster")),
            new(SpeakerGranny, "Later, you will clean different kinds of pollution in different ways. For now, one piece at a time.", "npc1giving"),
            new(SpeakerShimmer, "I will. I'll remember every light we bring back.", "npc1giving"),
        });

        d[Starfish] = new DialogueTimeline(Starfish, new List<DialogueLine>
        {
            new(SpeakerStarfish, "Wow... I haven't seen another glowing jellyfish in a long time.", "starfish"),
            new(SpeakerStarfish, "There's plastic everywhere. Animals get trapped in it; some even mistake it for food.", "starfish"),
            new(SpeakerShimmer, "What should I watch out for?", null, new List<DialogueChoice>
            {
                new("Tell me the worst part.", SetFlag: "starfish_more", GotoLabel: "starfish_more"),
                new("I get it. I need to clean the plastic.", SetFlag: "starfish_ready", GotoLabel: "starfish_continue"),
            }),
            new(SpeakerStarfish, "The worst part is that it looks like food. No one means to eat trash, but hungry animals still do.", "starfish", Label: "starfish_more"),
            new(SpeakerStarfish, "Far above us, seabirds feed plastic to their chicks without knowing. Their stomachs fill up, and there is no room left for real food.", "starfish",
                Label: "starfish_continue",
                Picture: DialoguePicture.Photo("ch1_albatross_debris.jpg",
                    "A Laysan albatross chick among plastic debris on Midway Atoll.", "NOAA Marine Debris Program")),
            new(SpeakerStarfish, "Seaweed is farther in, near the darker water. Talk to them before you face the big pile.", "starfish",
                Picture: new(AssetLoader.NpcPortrait("seaweed"), "Seaweed · deeper along the reef")),
        });

        d[Seaweed] = new DialogueTimeline(Seaweed, new List<DialogueLine>
        {
            new(SpeakerSeaweed, "Wait... Do you hear that low, rumbling sound?", "seaweed"),
            new(SpeakerSeaweed, "That sound is coming from a group of young jellyfish. Wastewater tangled their bodies together.", "seaweed"),
            new(SpeakerSeaweed, "Do you want the simple plan?", "seaweed", new List<DialogueChoice>
            {
                new("Yes. Keep it simple.", SetFlag: "seaweed_plan", GotoLabel: "seaweed_plan"),
                new("I can figure it out.", SetFlag: "seaweed_skip_plan", GotoLabel: "seaweed_continue"),
            }),
            new(SpeakerSeaweed, "Clean the smaller plastic first. Then stand far enough away and press 1. Do not swim into the pile.", "seaweed", Label: "seaweed_plan"),
            new(SpeakerSeaweed, "Use your light on the smaller trash first. Then press 1 on the loosest part of the big pile.", "seaweed", Label: "seaweed_continue"),
            new(SpeakerSeaweed, "If it moves toward you, back up. Cleaning it does not mean hugging it.", "seaweed"),
        });

        d[JellyfishBox] = new DialogueTimeline(JellyfishBox, new List<DialogueLine>
        {
            new(SpeakerShimmer, "How did this get here... an old, rusty barrel?"),
            new(SpeakerBox, "I used to transport fuel to factories on land.", "barrel"),
            new(SpeakerBox, "Once they were done with me, they decided it was easier to just toss me into the sea.", "barrel"),
            new(SpeakerShimmer, "What do you want me to do?", null, new List<DialogueChoice>
            {
                new("Ask about the leak.", SetFlag: "barrel_leak", GotoLabel: "barrel_leak"),
                new("Just record it.", SetFlag: "barrel_record", GotoLabel: "barrel_record"),
            }),
            new(SpeakerBox, "Do not push me around. Mark where I am, then let someone remove me safely.", "barrel", Label: "barrel_leak"),
            new(SpeakerBox, "I don't want to keep leaking.", "barrel"),
            new(SpeakerBox, "Writing it down still matters. If no one records it, people pretend it was never here.", "barrel", Label: "barrel_record"),
            new(SpeakerBox, "The oil sticks to animals and blocks out the light; it even harms the fish before it spreads any further.", "barrel"),
            new(SpeakerShimmer, "I'll save its story in my journal. Even an old oil drum deserves a better ending."),
        });

        d[BossDefeated] = new DialogueTimeline(BossDefeated, new List<DialogueLine>
        {
            new(SpeakerShimmer, "The pile is breaking apart!"),
            new(SpeakerShimmer, "The trapped animals are returning to the current."),
            new(SpeakerShimmer, "The plastic is gone. Something clean is moving in the current now."),
        });

        return d;
    }
}

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

/// A picture shown inside a line's bubble. Either the game's own art (who to find,
/// what to pick up, where you are going next) or a real photograph that shows the
/// harm the line is talking about. Photos always carry a credit; art never does.
public sealed record DialoguePicture(string Path, string Caption, string? Credit = null)
{
    public bool IsPhoto => Credit != null;

    /// A public-domain photo from res://assets/photos (see assets/photos/CREDITS.md).
    public static DialoguePicture Photo(string file, string caption, string credit) =>
        new($"res://assets/photos/{file}", caption, credit);
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
    public const string SpeakerBox = "Oil Barrel";
    public const string SpeakerHermit = "Old Zheng";
    public const string SpeakerLantern = "Lanternfish";
    public const string SpeakerShoal = "Lost Shoal";
    public const string SpeakerBoss = "Brood Mother";
    public const string SpeakerFrostshell = "Frostshell Guardian";
    public const string SpeakerCore = "Overheated Core";

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
            new(SpeakerGranny, "Your light is still clean, Shimmer.", "npc1giving"),
            new(SpeakerGranny, "Different kinds of pollution need different elements.", "npc1giving"),
            new(SpeakerGranny, "First, I will teach you to use Water.", "npc1giving"),
            new(SpeakerGranny, "Will you help me restore this nursery?", "npc1giving", new List<DialogueChoice>
            {
                new("I'll try.", SetFlag: "granny_accept", GotoLabel: "granny_yes"),
                new("I'm still scared.", SetFlag: "granny_hesitate", GotoLabel: "granny_soft"),
            }),
            new(SpeakerGranny, "That makes sense. You do not have to be fearless. I will guide you through the first current.", "npc1giving", Label: "granny_soft"),
            new(SpeakerGranny, "The Water Shards are hiding inside the rubbish that drifted in. Gather them and the water will answer you.", "npc1giving", Label: "granny_yes",
                Picture: new(AssetLoader.ChapterElement(1), "Look for these · 8 are scattered through the Shallows")),
            new(SpeakerGranny, "Then, at the Brood Mother, press 1 to release Water.", "npc1giving"),
            new(SpeakerGranny, "Ice and Electric currents wait farther below. One step at a time.", "npc1giving"),
            new(SpeakerShimmer, "I will remember every light we bring back.", "npc1giving"),
        });

        d[Starfish] = new DialogueTimeline(Starfish, new List<DialogueLine>
        {
            new(SpeakerStarfish, "I have not seen another living light in a long time.", "starfish"),
            new(SpeakerStarfish, "Plastic can trap animals, or be mistaken for food.", "starfish"),
            new(SpeakerStarfish, "Far above us, seabirds feed it to their chicks without knowing. Their stomachs fill up, and there is no room left for real food.", "starfish",
                Picture: DialoguePicture.Photo("ch1_albatross_debris.jpg",
                    "A Laysan albatross chick among plastic debris on Midway Atoll.", "NOAA Marine Debris Program")),
            new(SpeakerStarfish, "Seaweed heard something moving below. Talk to them before you go deeper.", "starfish",
                Picture: new(AssetLoader.NpcPortrait("seaweed"), "Seaweed · a little further along the reef")),
        });

        d[Seaweed] = new DialogueTimeline(Seaweed, new List<DialogueLine>
        {
            new(SpeakerSeaweed, "Can you hear that low pulse?", "seaweed"),
            new(SpeakerSeaweed, "It was once a group of young jellyfish. Wastewater fused them together.", "seaweed"),
            new(SpeakerSeaweed, "Collect the shards, then use Water on the thinnest part of the membrane.", "seaweed"),
        });

        d[JellyfishBox] = new DialogueTimeline(JellyfishBox, new List<DialogueLine>
        {
            new(SpeakerShimmer, "What is this... a rusted barrel?"),
            new(SpeakerBox, "I once carried fuel for a factory on land.", "barrel"),
            new(SpeakerBox, "They said dropping me into the sea was the easiest solution.", "barrel"),
            new(SpeakerBox, "I do not want to leak anymore. Please take this oil away.", "barrel"),
            new(SpeakerBox, "Oil can coat bodies, block light, and harm eggs before anyone sees the damage.", "barrel"),
            new(SpeakerShimmer, "I will keep its story in the Tidal Memories. Even this barrel wanted another ending."),
        });

        d[BossDefeated] = new DialogueTimeline(BossDefeated, new List<DialogueLine>
        {
            new(SpeakerShimmer, "The shell is opening."),
            new(SpeakerShimmer, "The trapped lights are returning to the water."),
            new(SpeakerShimmer, "A new element rises from the current. It will guide me to the next sea."),
        });

        return d;
    }
}

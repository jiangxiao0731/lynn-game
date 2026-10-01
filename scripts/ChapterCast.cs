using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ShallowSeaDream;

/// One placeable resident. `Id` doubles as the dialogue timeline key, the scene node
/// name and the log key, so a member only has to be listed here once.
public sealed record CastMember(
    string Id,
    string DisplayName,
    string PortraitId,
    int Chapter,
    Vector2 Position,
    Color Tint,
    IReadOnlyList<string> Lines,
    /// On-screen height in pixels. Set per character: a drifting fume is small, a
    /// pipe mouth or drum is heavy scenery. Not decoration — size is how the player
    /// reads what matters before reading any text.
    float DisplaySize = 118f,
    /// Pictures keyed by line index (see DialoguePicture), for lines about a real harm.
    Dictionary<int, DialoguePicture>? Pictures = null);

/// res://scripts/ChapterCast.cs
/// The supplied character paintings, placed as residents rather than enemies.
///
/// Every chapter resolves to exactly one guardian (AssetLoader.ChapterBoss) and one
/// collectable element (AssetLoader.ChapterElement); everything else painted for that
/// chapter lives here and is spoken to, not fought. Adding art is a one-line edit:
/// drop `res://assets/img/npc_{PortraitId}.png` in and append a record.
///
/// The lines follow NARRATIVE_ARCHITECTURE.md: notice the harm, trace the source,
/// restore a process, then name the limit of the repair. Each chapter's residents
/// carry one link of that chain each, so a player who talks to everyone assembles the
/// causal story, and a player who talks to nobody still finishes the level.
public static class ChapterCast
{
    /// Residents added on top of the hand-placed chapter-one cast.
    private static readonly List<CastMember> Members = new()
    {
        // ---------------------------------------------------------------
        // Chapter 1 — Tidepool Nursery. Harm: land waste reaching the sea.
        // ---------------------------------------------------------------
        new("bannerfish", "Bannerfish", "bannerfish", 1,
            new Vector2(2480, 470), new Color(0.86f, 0.82f, 0.62f),
            new[]
            {
                "I used to know this reef like a map. Every stripe showed me the way home.",
                "Now, plastic bags cover me; I can't see the path ahead at all.",
                "No one has cleaned this place up. It's buried under things people threw away on land.",
            }, 128f),


        // ---------------------------------------------------------------
        // Chapter 2 — Frozen Trench. Harm: runoff, algal bloom, oxygen loss.
        // Residents carry the chain in reading order, left to right.
        // ---------------------------------------------------------------
        new("ch2_testtube", "Overflow Pipe", "ch2_testtube", 2,
            new Vector2(520, 690), new Color(0.72f, 0.90f, 0.52f),
            new[]
            {
                "You're the first person in a long time to swim toward me instead of away.",
                "I fell into the sewer. Someone upstream had enough of me and acted as if whatever was left was the river's problem.",
                "If you really want to solve this, start further upstream and find my source. Put an end to my polluting the ocean.",
            }, 132f),

        new("ch2_drumcone", "Old Traffic Cone", "ch2_drumcone", 2,
            new Vector2(760, 860), new Color(0.78f, 0.80f, 0.42f),
            new[]
            {
                "I've sat here so long that the silt has basically made me part of the floor.",
                "The water above me is thick and green now. It was like that when I first arrived, too.",
                "The trench didn't just get cold. It lost its oxygen and slowly stopped breathing.",
            }, 150f),

        new("ch2_gascloud", "Algae Cloud", "ch2_gascloud", 2,
            new Vector2(1090, 250), new Color(0.70f, 0.92f, 0.46f),
            new[]
            {
                "I grew here after the water absorbed too many nutrients.",
                "Runoff brought nitrogen and phosphorus here, and the algae grew out of control.",
                "Then the algae died, and decomposing it used up all the oxygen.",
            }, 140f, new()
            {
                [1] = DialoguePicture.Photo("ch2_sediment_plume.jpg",
                    "River water loaded with soil and nutrients pours into the Gulf of Mexico.", "NASA Goddard Space Flight Center"),
            }),

        new("ch2_radiodrum", "Warning Barrel", "ch2_radiodrum", 2,
            new Vector2(1330, 845), new Color(0.80f, 0.76f, 0.36f),
            new[]
            {
                "Someone painted a warning on me so people would stay away.",
                "But they were still afraid, so they dumped me where no one goes—making the warning irrelevant.",
                "The fish here can't read it. People chose what was easiest and ignored the consequences.",
            }, 168f),

        new("ch2_jerrycan", "Leaking Can", "ch2_jerrycan", 2,
            new Vector2(1510, 700), new Color(0.74f, 0.78f, 0.44f),
            new[]
            {
                "I leak a little bit every day. It's hardly enough for anyone to call it a real accident.",
                "A lot of pollution happens like this: small leaks from many places, day after day.",
                "Clean the leak from me, and your light will get stronger. Stop the other leaks, and this place can stay better.",
            }, 146f),

        new("ch2_maskbeast", "Masked Sea Dog", "ch2_maskbeast", 2,
            new Vector2(1890, 800), new Color(0.68f, 0.82f, 0.50f),
            new[]
            {
                "I wear this mask all the time because the water here isn't safe to breathe anymore.",
                "Low oxygen doesn't look like poison. It just looks like an empty space where life used to be.",
                "But so many ocean creatures can't wear masks. That's why we have to restore the currents.",
            }, 158f, new()
            {
                [1] = DialoguePicture.Photo("ch2_dead_zone.jpg",
                    "A 2011 survey of the Gulf of Mexico 'dead zone'. Red marks seabed water with too little oxygen for most life.", "NOAA"),
            }),

        new("ch2_flask", "Broken Bottle", "ch2_flask", 2,
            new Vector2(2090, 300), new Color(0.76f, 0.88f, 0.48f),
            new[]
            {
                "A crack appeared in my side. Everything I was supposed to hold leaked right out.",
                "I used to have a label, but it fell off in the water. Now, no one even knows what I am.",
                "Wake the Anchor. Then clean what's leaking from me, so it can be safely processed.",
            }, 126f),

        new("ch2_wastebag", "Trash Bag", "ch2_wastebag", 2,
            new Vector2(2270, 850), new Color(0.70f, 0.74f, 0.40f),
            new[]
            {
                "They tied me up, weighed me down, and let me sink. But hiding something doesn't make it disappear.",
                "I've been slowly coming undone for years; the trench has absorbed everything that leaked out.",
                "You can't undo the fact that I was cast aside here. But you can stop the next bag from leaving the shore.",
            }, 152f),


        new("ch2_vent", "Drainpipe", "ch2_vent", 2,
            new Vector2(2520, 815), new Color(0.66f, 0.78f, 0.46f),
            new[]
            {
                "Almost everything you see here came through me.",
                "I didn't choose what I carried. I was born to accept whatever people discharged into me.",
                "The Chemical Waste Monster formed where the leaks collected. Clean the smaller leaks first, then face it.",
            }, 210f),

        // ---------------------------------------------------------------
        // Chapter 3 — The Old Lighthouse. Harm: oil, ocean heat, bleaching.
        // ---------------------------------------------------------------
        new("ch3_slickblob", "Oil Slick", "ch3_slickblob", 3,
            new Vector2(560, 430), new Color(0.92f, 0.62f, 0.34f),
            new[]
            {
                "Watch out. If you touch me, I'll spread—and I don't break down or vanish easily.",
                "Oil causes immense damage instantly. It coats animals, harms living things, and lingers in the habitat.",
                "This reef is already too hot. I am simply the damage that people can actually see.",
            }, 138f, new()
            {
                [1] = DialoguePicture.Photo("ch3_oiled_pelican.jpg",
                    "A wildlife officer reaches an oiled brown pelican after the 2010 Deepwater Horizon spill.", "U.S. Fish and Wildlife Service"),
            }),

        new("ch3_reddrum", "Fuel Drum", "ch3_reddrum", 3,
            new Vector2(830, 845), new Color(0.90f, 0.44f, 0.36f),
            new[]
            {
                "I used to power the lighthouse. When the light went out, they left me on the seabed.",
                "The keeper said they'd come back for me. Then the reef was devastated, and they never returned.",
                "Let the lighthouse run on cleaner equipment. Then I won't be the only fuel this coast relies on.",
            }, 172f),

        new("ch3_pufferfish", "Masked Pufferfish", "ch3_pufferfish", 3,
            new Vector2(1160, 500), new Color(0.86f, 0.72f, 0.44f),
            new[]
            {
                "I puff up to look dangerous, but here, that just gives the oil more surface area to cling to.",
                "That coral was my friend. One summer, it lost its color and never got it back. We now know it was bleaching.",
                "It didn't happen all at once. Water temperatures remained high for far too long, causing the coral to lose the nutrients it relied on to survive.",
            }, 130f, new()
            {
                [1] = DialoguePicture.Photo("ch3_bleaching.jpg",
                    "A bleached brain coral in the Florida Keys during the 2023 marine heatwave.", "NOAA"),
            }),

        new("ch3_seahorse", "Seahorse", "ch3_seahorse", 3,
            new Vector2(1430, 640), new Color(0.88f, 0.58f, 0.40f),
            new[]
            {
                "I'm clinging tightly to this nozzle because there are no living creatures left nearby for me to hold onto.",
                "Seahorses need something to anchor themselves to. The seagrass that once grew here withered and died in the heat.",
                "If the relay is reconnected, the water temperature will start to drop. But it takes time—the recovery rate of the coral reefs and seagrass is far slower than we'd hoped.",
            }, 142f),

        new("ch3_cancrab", "Can-Shell Crab", "ch3_cancrab", 3,
            new Vector2(1640, 860), new Color(0.90f, 0.50f, 0.32f),
            new[]
            {
                "This isn't my real shell. My old one became too fragile in the warm water, so I had to make do with whatever I could find right here.",
                "Half of us are using trash as shells. After all, without them, I'd be left completely unprotected.",
                "You alone can't cool down the entire ocean. But humans can stop dumping so much heat into it.",
            }, 150f),

        new("ch3_slickray", "Oil-Stained Ray", "ch3_slickray", 3,
            new Vector2(2030, 280), new Color(0.82f, 0.64f, 0.86f),
            new[]
            {
                "People see the colors on my back and think they're beautiful.",
                "But it's actually just a thin film of oil that spreads wherever I swim.",
                "Those seemingly beautiful colors are actually a sign of just how far the destruction has spread.",
            }, 156f, new()
            {
                [1] = DialoguePicture.Photo("ch3_oil_slick.jpg",
                    "Oil from the Deepwater Horizon spill smoothing the surface of the Gulf, seen by satellite in June 2010.",
                    "NASA Goddard Space Flight Center"),
            }),

        new("ch3_coraltar", "Oil-Covered Coral", "ch3_coraltar", 3,
            new Vector2(2310, 855), new Color(0.78f, 0.46f, 0.38f),
            new[]
            {
                "There is still living coral beneath this layer of gunk. It just can't reach the sunlight anymore.",
                "If the oil is cleaned off, it will try to grow again. You see, we've never stopped trying.",
                "That's the nature of a coral reef—it's always fighting to recover. It just needs enough time.",
            }, 176f),

        new("ch3_oilflame", "Oil Flare", "ch3_oilflame", 3,
            new Vector2(2570, 815), new Color(0.94f, 0.56f, 0.28f),
            new[]
            {
                "This is what combustion looks like—when it happens underwater rather than above the surface.",
                "Greenhouse gases trap heat, and the ocean absorbs most of it.",
                "You didn't come only to put me out. Clean the oil around us first, then stop the monster that grew from it.",
            }, 164f, new()
            {
                [1] = DialoguePicture.Photo("ch3_rig_fire.jpg",
                    "The Deepwater Horizon rig burning, April 2010. It sank two days later, and oil leaked for 87 days.", "U.S. Coast Guard"),
            }),

    };

    /// Residents belonging to one chapter, in left-to-right reading order.
    public static IReadOnlyList<CastMember> For(int chapter) =>
        Members.Where(m => m.Chapter == chapter).OrderBy(m => m.Position.X).ToList();

    public static IReadOnlyList<CastMember> All => Members;

    /// Timelines for every member, folded into NarrativeData so `Play(id)` just works.
    public static IEnumerable<KeyValuePair<string, DialogueTimeline>> Timelines()
    {
        foreach (var m in Members)
        {
            var lines = new List<DialogueLine>();
            if (m.Lines.Count > 0)
                lines.Add(MakeLine(m, 0));

            if (m.Lines.Count > 1)
            {
                string detailLabel = $"{m.Id}_detail";
                string continueLabel = $"{m.Id}_continue";
                lines.Add(new DialogueLine(m.DisplayName, ChoicePrompt(m), m.PortraitId, new List<DialogueChoice>
                {
                    new(ChoiceA(m), SetFlag: $"{m.Id}_asked", GotoLabel: detailLabel),
                    new(ChoiceB(m), SetFlag: $"{m.Id}_ready", GotoLabel: continueLabel),
                }));
                lines.Add(new DialogueLine(m.DisplayName, ChoiceDetail(m), m.PortraitId, Label: detailLabel));
                lines.Add(MakeLine(m, 1, continueLabel));
                for (int i = 2; i < m.Lines.Count; i++) lines.Add(MakeLine(m, i));
            }

            yield return new KeyValuePair<string, DialogueTimeline>(m.Id, new DialogueTimeline(m.Id, lines));
        }
    }

    private static DialogueLine MakeLine(CastMember member, int index, string? label = null) =>
        new(member.DisplayName, member.Lines[index], member.PortraitId, Label: label,
            Picture: member.Pictures != null && member.Pictures.TryGetValue(index, out var pic) ? pic : null);

    private static string ChoicePrompt(CastMember member) => member.Id switch
    {
        "ch2_testtube" => "Should I trace where you came from?",
        "ch2_drumcone" => "Is the green water the real problem?",
        "ch2_gascloud" => "How did you grow so fast?",
        "ch2_radiodrum" => "Did the warning label help anyone?",
        "ch2_jerrycan" => "Can a small leak really matter?",
        "ch2_maskbeast" => "Why does low oxygen feel so scary?",
        "ch2_flask" => "What should I do with what leaked out?",
        "ch2_wastebag" => "If you are hidden, are you still dangerous?",
        "ch2_vent" => "So this all came through the drain?",
        "ch3_slickblob" => "What makes oil so hard to clean?",
        "ch3_reddrum" => "Was this lighthouse always powered this way?",
        "ch3_pufferfish" => "What happened to the coral?",
        "ch3_seahorse" => "Why are you holding onto trash?",
        "ch3_cancrab" => "Is that can really your shell?",
        "ch3_slickray" => "Are those colors oil?",
        "ch3_coraltar" => "Can the coral still recover?",
        "ch3_oilflame" => "What does this fire mean?",
        "bannerfish" => "Do you still remember the path?",
        _ => member.Chapter switch
        {
            3 => "Do you want the part people usually miss?",
            _ => "Do you want me to say it plainly?",
        },
    };

    private static string ChoiceA(CastMember member) => member.Id switch
    {
        "ch2_testtube" => "Yes. Find the source.",
        "ch2_drumcone" => "Explain the green water.",
        "ch2_gascloud" => "Tell me what fed you.",
        "ch2_radiodrum" => "Tell me what went wrong.",
        "ch2_jerrycan" => "Yes, small leaks count.",
        "ch2_maskbeast" => "Explain low oxygen.",
        "ch2_flask" => "Tell me the safe way.",
        "ch2_wastebag" => "Yes, hiding is not fixing.",
        "ch2_vent" => "Trace the pipe.",
        "ch3_slickblob" => "Explain the oil.",
        "ch3_reddrum" => "Talk about the fuel.",
        "ch3_pufferfish" => "Tell me about bleaching.",
        "ch3_seahorse" => "Explain the missing seagrass.",
        "ch3_cancrab" => "Tell me about the shell.",
        "ch3_slickray" => "Explain the slick.",
        "ch3_coraltar" => "Tell me if it can heal.",
        "ch3_oilflame" => "Explain the heat.",
        "bannerfish" => "Tell me the path.",
        _ => "Yes, tell me.",
    };

    private static string ChoiceB(CastMember member) => member.Id switch
    {
        "ch2_testtube" => "I'll keep moving upstream.",
        "ch2_drumcone" => "I see. The trench can't breathe.",
        "ch2_gascloud" => "I get it. Too many nutrients.",
        "ch2_radiodrum" => "The warning came too late.",
        "ch2_jerrycan" => "I'll clean the leak.",
        "ch2_maskbeast" => "I'll restore the current.",
        "ch2_flask" => "I'll contain it.",
        "ch2_wastebag" => "I'll stop the next one.",
        "ch2_vent" => "I'll clean what came through.",
        "ch3_slickblob" => "I'll keep my distance.",
        "ch3_reddrum" => "The lighthouse needs safer power.",
        "ch3_pufferfish" => "The reef needs time.",
        "ch3_seahorse" => "I'll bring safer current back.",
        "ch3_cancrab" => "Trash is not protection.",
        "ch3_slickray" => "I'll clean the oil.",
        "ch3_coraltar" => "I'll give it a chance.",
        "ch3_oilflame" => "I'll stop the source.",
        "bannerfish" => "I'll clean the plastic.",
        _ => "I get it.",
    };

    private static string ChoiceDetail(CastMember member) => member.Id switch
    {
        "ch2_testtube" => "Pollution here did not start in the trench. It rode down from somewhere people thought was far away.",
        "ch2_drumcone" => "The green water looks alive, but too much of it can make the whole place run out of oxygen.",
        "ch2_gascloud" => "I did not appear from nowhere. Extra nutrients fed me until I covered the water.",
        "ch2_radiodrum" => "A warning sign only helps if people still choose to handle the waste safely.",
        "ch2_jerrycan" => "One leak is small. A hundred small leaks become the water everyone has to live in.",
        "ch2_maskbeast" => "Low oxygen is quiet. Fish do not always get a dramatic warning; they just cannot breathe.",
        "ch2_flask" => "Do not scatter it. Contain it first, then let the current carry it to a safer place.",
        "ch2_wastebag" => "Throwing something out of sight only moves the problem to someone else's home.",
        "ch2_vent" => "The drain connects land and sea. What goes in upstream can become a monster down here.",
        "ch3_slickblob" => "Oil spreads thin and fast. Even a small patch can coat feathers, gills, and coral.",
        "ch3_reddrum" => "Old fuel solved one problem and created another. The lighthouse needs a cleaner way to shine.",
        "ch3_pufferfish" => "Bleaching means the coral lost the tiny helpers it depends on for food.",
        "ch3_seahorse" => "When seagrass dies, small animals lose the places they use to rest and hide.",
        "ch3_cancrab" => "Using trash as shelter is survival, but it should not be normal.",
        "ch3_slickray" => "A rainbow film can look pretty, but it is still a sign that oil is spreading.",
        "ch3_coraltar" => "Coral can fight back slowly, but only if oil and heat stop hitting it again and again.",
        "ch3_oilflame" => "Burning fuel adds heat to a sea that is already carrying too much.",
        "bannerfish" => "The path is still here. It is just hidden under plastic that should never have reached the reef.",
        _ => member.Chapter switch
        {
            3 => "The reef is not weak. It is being hit by oil, heat, and time all at once.",
            _ => "People threw things away, and the ocean had to carry them.",
        },
    };
}

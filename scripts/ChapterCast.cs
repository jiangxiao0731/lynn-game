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
                "Clean me up, and the trench will improve. Stop the other leaks, and it can stay in better shape.",
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
                "Wake the Anchor. The currents can carry what's left of me to a place where I can be safely processed.",
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
                "The Chemical Waste Monster formed where the leaks collected. Stop the leaks, then let clean water flow again.",
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
                "You didn't come only to put me out. You came to stop the pollution that created me.",
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
            var lines = m.Lines
                .Select((text, i) => new DialogueLine(m.DisplayName, text, m.PortraitId,
                    Picture: m.Pictures != null && m.Pictures.TryGetValue(i, out var pic) ? pic : null))
                .ToList();
            yield return new KeyValuePair<string, DialogueTimeline>(m.Id, new DialogueTimeline(m.Id, lines));
        }
    }
}

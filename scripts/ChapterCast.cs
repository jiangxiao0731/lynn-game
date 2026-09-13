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
    float DisplaySize = 118f);

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
                "I used to read this reef like a map. Every stripe of it told me where home was.",
                "Now bags drift through the same shapes, and I lead the young ones the wrong way.",
                "Nothing here was hunted away. It was buried under things that were thrown out on land.",
            }, 128f),


        // ---------------------------------------------------------------
        // Chapter 2 — Frostbound Trench. Harm: runoff, algal bloom, oxygen loss.
        // Residents carry the chain in reading order, left to right.
        // ---------------------------------------------------------------
        new("ch2_testtube", "Spill Tube", "ch2_testtube", 2,
            new Vector2(520, 690), new Color(0.72f, 0.90f, 0.52f),
            new[]
            {
                "You are the first one in a long time to swim toward me instead of away.",
                "I came down a drain. Somebody upstream finished with me and the rest was the river's problem.",
                "Whatever you are here to restore, start further up than me. I am only where it settled.",
            }, 132f),

        new("ch2_drumcone", "Settled Cone", "ch2_drumcone", 2,
            new Vector2(760, 860), new Color(0.78f, 0.80f, 0.42f),
            new[]
            {
                "I have sat here so long the silt has made me part of the floor.",
                "The water above me is thick and green now. It was clear when I arrived.",
                "The trench did not get colder. It got hungrier, and then it stopped breathing.",
            }, 150f),

        new("ch2_gascloud", "Green Haze", "ch2_gascloud", 2,
            new Vector2(1090, 250), new Color(0.70f, 0.92f, 0.46f),
            new[]
            {
                "I am not smoke. I am what the water grew when it was fed too much.",
                "Runoff carried nitrogen and phosphorus down here, and the algae bloomed on it.",
                "Then the bloom died, and rotting it used up the oxygen. That is the part nobody sees.",
            }, 140f),

        new("ch2_radiodrum", "Marked Drum", "ch2_radiodrum", 2,
            new Vector2(1330, 845), new Color(0.80f, 0.76f, 0.36f),
            new[]
            {
                "Someone painted a warning on me so people would keep their distance.",
                "Then they put me where no people go, and the warning stopped meaning anything.",
                "The fish here cannot read. That is the whole trick of dumping things underwater.",
            }, 168f),

        new("ch2_jerrycan", "Leaking Can", "ch2_jerrycan", 2,
            new Vector2(1470, 855), new Color(0.74f, 0.78f, 0.44f),
            new[]
            {
                "I leak a little every day. Not enough for anyone to call it an accident.",
                "That is how most of this happened. Small amounts, for a long time, from many places.",
                "Clean me up and the trench improves. Stop the ones still leaking and it stays that way.",
            }, 146f),

        new("ch2_maskbeast", "Filter Hound", "ch2_maskbeast", 2,
            new Vector2(1890, 800), new Color(0.68f, 0.82f, 0.50f),
            new[]
            {
                "I keep this mask on because the water here is not breathable any more.",
                "Low oxygen does not look like poison. It looks like an empty place where things used to live.",
                "The eggs in the nursery cannot wear a mask. That is why the current has to come back.",
            }, 158f),

        new("ch2_flask", "Cracked Flask", "ch2_flask", 2,
            new Vector2(2090, 300), new Color(0.76f, 0.88f, 0.48f),
            new[]
            {
                "There is a crack along my side. Everything I was supposed to contain went out through it.",
                "I was labelled once. The label came off in the water, so nobody knows what to do with me.",
                "Wake the anchors and the current will carry the rest of me somewhere it can be handled.",
            }, 126f),

        new("ch2_wastebag", "Bagged Waste", "ch2_wastebag", 2,
            new Vector2(2270, 850), new Color(0.70f, 0.74f, 0.40f),
            new[]
            {
                "Tied shut, weighted down, and sunk. Out of sight is not the same as gone.",
                "I have been opening slowly for years. The trench has been swallowing it the whole time.",
                "You cannot un-sink me. You can make sure the next one never leaves the shore.",
            }, 152f),


        new("ch2_vent", "Pipe Mouth", "ch2_vent", 2,
            new Vector2(2750, 815), new Color(0.66f, 0.78f, 0.46f),
            new[]
            {
                "I am the opening. Everything you have met down here came through me.",
                "I did not decide what to carry. I was built to carry whatever I was given.",
                "The Guardian ahead froze this trench to hold me back. Free the water and she can finally rest.",
            }, 210f),

        // ---------------------------------------------------------------
        // Chapter 3 — The Silent Lighthouse. Harm: oil, ocean heat, bleaching.
        // ---------------------------------------------------------------
        new("ch3_slickblob", "Slick Drifter", "ch3_slickblob", 3,
            new Vector2(560, 430), new Color(0.92f, 0.62f, 0.34f),
            new[]
            {
                "Careful. I spread when I am touched, and I do not come apart again.",
                "Oil does three things at once: it coats bodies, it ruins eggs, and it stays in the habitat.",
                "The reef here was already too warm. I was just the part that finally showed.",
            }, 138f),

        new("ch3_reddrum", "Fuel Drum", "ch3_reddrum", 3,
            new Vector2(830, 845), new Color(0.90f, 0.44f, 0.36f),
            new[]
            {
                "I powered the lighthouse once. When the lamp went out, they left me on the seabed.",
                "The keepers meant to come back for me. Then there was nothing here worth coming back for.",
                "Get the light running on something cleaner, and I stop being the only fuel this coast has.",
            }, 172f),

        new("ch3_pufferfish", "Masked Puffer", "ch3_pufferfish", 3,
            new Vector2(1160, 500), new Color(0.86f, 0.72f, 0.44f),
            new[]
            {
                "I puff up to look dangerous. Down here it just makes me easier to coat.",
                "The coral went pale one summer and never got its colour back. We call it bleaching now.",
                "It was not sudden. The water was too warm for too long, and the coral let go of what fed it.",
            }, 130f),

        new("ch3_seahorse", "Pump Seahorse", "ch3_seahorse", 3,
            new Vector2(1430, 640), new Color(0.88f, 0.58f, 0.40f),
            new[]
            {
                "I hold on to this nozzle because there is nothing living left to hold on to.",
                "Seahorses need something to anchor to. The grass that used to be here cooked off.",
                "Reconnect the relays and the current cools. Slowly. Recovery does not run at our speed.",
            }, 142f),

        new("ch3_cancrab", "Canister Crab", "ch3_cancrab", 3,
            new Vector2(1640, 860), new Color(0.90f, 0.50f, 0.32f),
            new[]
            {
                "This shell is not mine. My old one softened in the warm water and I had to take what was here.",
                "Half of us are wearing rubbish now. It is that, or wear nothing at all.",
                "You cannot cool an ocean by hand. You can stop adding heat to it.",
            }, 150f),

        new("ch3_slickray", "Rainbow Ray", "ch3_slickray", 3,
            new Vector2(2030, 280), new Color(0.82f, 0.64f, 0.86f),
            new[]
            {
                "People see the colours on my back and think something beautiful is happening.",
                "It is a film of oil one drop thick, spread over everything I swim past.",
                "The prettiest part of this damage is the part that tells you how far it has already gone.",
            }, 156f),

        new("ch3_coraltar", "Tarred Coral", "ch3_coraltar", 3,
            new Vector2(2310, 855), new Color(0.78f, 0.46f, 0.38f),
            new[]
            {
                "There is still coral under me. It is alive. It just cannot reach the light.",
                "Take the weight off and it will try again. It has been trying the whole time.",
                "That is the thing about a reef. It does not give up, it only runs out of time.",
            }, 176f),

        new("ch3_oilflame", "Oil Flare", "ch3_oilflame", 3,
            new Vector2(2570, 815), new Color(0.94f, 0.56f, 0.28f),
            new[]
            {
                "I am what burning looks like when it happens under water instead of over it.",
                "Greenhouse gases hold the heat in, and the ocean takes up most of what is trapped.",
                "You are not here to put me out. You are here to stop the reason I was lit.",
            }, 164f),

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
                .Select(text => new DialogueLine(m.DisplayName, text, m.PortraitId))
                .ToList();
            yield return new KeyValuePair<string, DialogueTimeline>(m.Id, new DialogueTimeline(m.Id, lines));
        }
    }
}

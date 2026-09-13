using System.Collections.Generic;
using Godot;

namespace ShallowSeaDream;

/// res://scripts/ChapterMap.cs
/// Chapter geometry derived from the painted backdrop rather than a fixed constant.
///
/// Each chapter is three paintings laid head to tail. They are roughly 6000x1080, so
/// squeezing them into the original 3600-wide map crushed them about five times
/// horizontally. Instead the map takes its length from the art: every panel is drawn
/// at its own aspect ratio, and the level becomes one long ~17000px crossing.
///
/// Level layouts were authored against the old 3600 width, so `ScaleX` restretches
/// those hand-picked X coordinates over the new length; relative pacing is preserved
/// and nothing has to be repositioned by hand.
public static class ChapterMap
{
    public const float Height = 1080f;

    /// The width every hand-authored X coordinate in the controllers was placed against.
    public const float AuthoredWidth = 3600f;

    /// Used when a painting is missing so the level still has sane bounds.
    private const float FallbackPanelWidth = 6000f;

    private sealed record Layout(float[] Widths, float[] Starts, float Total);

    private static readonly Dictionary<int, Layout> Cache = new();

    private static Layout Get(int chapter)
    {
        chapter = Mathf.Clamp(chapter, 1, 3);
        if (Cache.TryGetValue(chapter, out var cached)) return cached;

        var widths = new float[3];
        var starts = new float[3];
        float cursor = 0f;
        for (int i = 0; i < 3; i++)
        {
            var tex = AssetLoader.Texture(AssetLoader.ChapterBackground(chapter, i));
            // Draw the panel at its own aspect ratio, sized to the map height.
            widths[i] = tex != null && tex.GetHeight() > 0
                ? Mathf.Round(tex.GetWidth() * Height / tex.GetHeight())
                : FallbackPanelWidth;
            starts[i] = cursor;
            cursor += widths[i];
        }

        var layout = new Layout(widths, starts, cursor);
        Cache[chapter] = layout;
        return layout;
    }

    public static float PanelWidth(int chapter, int index) => Get(chapter).Widths[Mathf.Clamp(index, 0, 2)];

    public static float PanelStart(int chapter, int index) => Get(chapter).Starts[Mathf.Clamp(index, 0, 2)];

    /// Full crossing length of the chapter, i.e. the three panels end to end.
    public static float TotalWidth(int chapter) => Get(chapter).Total;

    /// Factor that maps an authored X coordinate onto the long map.
    public static float ScaleX(int chapter) => TotalWidth(chapter) / AuthoredWidth;

    /// Stretch a hand-authored point horizontally; vertical placement is unchanged.
    public static Vector2 Place(int chapter, Vector2 authored)
        => new(authored.X * ScaleX(chapter), authored.Y);

    /// Stretch a hand-authored rectangle horizontally.
    public static Rect2 Place(int chapter, Rect2 authored)
    {
        float s = ScaleX(chapter);
        return new Rect2(authored.Position.X * s, authored.Position.Y, authored.Size.X * s, authored.Size.Y);
    }

    // --- The chapter's right end. The guardian belongs at the far edge of the map,
    // with the exit just past it and the arena mouth a readable run before it.

    /// Distance from the right edge back to the guardian.
    public const float BossInset = 1750f;
    /// Distance from the right edge back to the chapter exit.
    public const float ExitInset = 300f;
    /// Distance from the right edge back to the arena entrance.
    public const float ArenaGateInset = 4600f;

    public static Vector2 BossPoint(int chapter, float y = 540f)
        => new(TotalWidth(chapter) - BossInset, y);

    public static Vector2 ExitPoint(int chapter, float y = 540f)
        => new(TotalWidth(chapter) - ExitInset, y);

    public static float ArenaGateX(int chapter) => TotalWidth(chapter) - ArenaGateInset;

    /// True when `point` is far enough from every character/prop in `groups` that a
    /// pickup dropped there will not sit on top of one. Distances are in world pixels.
    public static bool IsClearOfOccupants(Node context, Vector2 point, float minDistance,
        params string[] groups)
    {
        foreach (var group in groups)
        foreach (var node in context.GetTree().GetNodesInGroup(group))
        {
            if (node is not Node2D occupant) continue;
            if (occupant.GlobalPosition.DistanceTo(point) < minDistance) return false;
        }
        return true;
    }

    /// Which of the three panels a world X sits in.
    public static int ZoneAt(int chapter, float x)
    {
        var l = Get(chapter);
        if (x >= l.Starts[2]) return 2;
        return x >= l.Starts[1] ? 1 : 0;
    }
}

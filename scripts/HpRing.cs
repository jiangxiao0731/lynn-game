using Godot;

namespace ShallowSeaDream;

/// res://scripts/HpRing.cs
/// Procedural circular HP ring (DESIGN_BRIEF §8): teal fill arc over a sand
/// background ring, starting at 12 o'clock and sweeping clockwise.
public partial class HpRing : Control
{
    private float _ratio = 1.0f;

    public void SetValue(int current, int max)
    {
        _ratio = max > 0 ? Mathf.Clamp((float)current / max, 0f, 1f) : 0f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = Size * 0.5f;
        float radius = Mathf.Min(Size.X, Size.Y) * 0.42f;
        float width = radius * 0.20f;

        // Faint deep-sea track ring.
        DrawArc(center, radius, 0, Mathf.Tau, 72,
            new Color(Palette.SurfaceLight.R, Palette.SurfaceLight.G, Palette.SurfaceLight.B, 0.18f), width);

        // Inner shimmer orb — glows brighter at full health, dims as the tide ebbs.
        var orb = Palette.DeepSea.Lerp(Palette.CoastalCyan, 0.20f + 0.35f * _ratio);
        DrawCircle(center, radius - width, new Color(orb.R, orb.G, orb.B, 0.55f));

        if (_ratio <= 0f) return;

        // Bioluminescent fill arc, 12 o'clock (-PI/2) sweeping clockwise. Cyan→teal by
        // health; WARNING_AMBER only when critically low (the player-damage signal).
        float start = -Mathf.Pi / 2f;
        float end = start + Mathf.Tau * _ratio;
        Color fill = _ratio <= 0.25f
            ? Palette.WarningAmber
            : Palette.PollutedTeal.Lerp(Palette.CoastalCyan, _ratio);

        // Soft outer glow, then the crisp arc on top.
        DrawArc(center, radius, start, end, 72, new Color(fill.R, fill.G, fill.B, 0.30f), width * 1.8f);
        DrawArc(center, radius, start, end, 72, fill, width);

        // A small leading bead at the arc head.
        var head = center + new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * radius;
        DrawCircle(head, width * 0.55f, new Color(1f, 1f, 1f, 0.85f));
    }
}

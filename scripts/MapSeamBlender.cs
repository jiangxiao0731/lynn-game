using Godot;

namespace ShallowSeaDream;

/// A non-gameplay visual pass that softens the joins between painted background
/// panels. The source images are not perfectly tileable, so a low-alpha underwater
/// wash plus a few horizontal current strokes makes the transition feel intentional
/// instead of like two flat images butted together.
public static class MapSeamBlender
{
    private const float WideWashWidth = 520f;
    private const float DeepWashWidth = 300f;

    public static void Add(Node2D map, float seamX, float mapHeight, int zIndex, Color tint, int seed)
    {
        AddWash(map, seamX, mapHeight, zIndex, WideWashWidth,
            new Color(tint.R, tint.G, tint.B, 0.00f),
            new Color(tint.Darkened(0.28f), 0.16f),
            "SeamSoftWash");

        // A second, narrower cool wash knocks back residual contrast at the exact
        // image edge. Its alpha stays low to avoid the old “white blurry column”.
        AddWash(map, seamX, mapHeight, zIndex + 1, DeepWashWidth,
            new Color(Palette.DeepSea.R, Palette.DeepSea.G, Palette.DeepSea.B, 0.00f),
            new Color(Palette.DeepSea.R, Palette.DeepSea.G, Palette.DeepSea.B, 0.11f),
            "SeamDepthWash");

        AddCurrentStrokes(map, seamX, mapHeight, zIndex + 2, tint, seed);
    }

    private static void AddWash(Node2D map, float seamX, float mapHeight, int zIndex,
        float width, Color edge, Color center, string name)
    {
        map.AddChild(new TextureRect
        {
            Name = name,
            Position = new Vector2(seamX - width * 0.5f, 0),
            Size = new Vector2(width, mapHeight),
            Texture = PlaceholderArt.HorizontalBand(edge, center),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            ZIndex = zIndex,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
    }

    private static void AddCurrentStrokes(Node2D map, float seamX, float mapHeight,
        int zIndex, Color tint, int seed)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)(seed + 53) * 1009UL };
        Color lineColor = new(
            Mathf.Lerp(Palette.CoastalCyan.R, tint.R, 0.42f),
            Mathf.Lerp(Palette.CoastalCyan.G, tint.G, 0.42f),
            Mathf.Lerp(Palette.CoastalCyan.B, tint.B, 0.42f),
            0.105f);

        for (int i = 0; i < 8; i++)
        {
            float width = rng.RandfRange(300f, 520f);
            float y = Mathf.Lerp(120f, mapHeight - 120f, (i + 0.5f) / 8f)
                      + rng.RandfRange(-34f, 34f);
            float amplitude = rng.RandfRange(8f, 22f);
            float phase = rng.RandfRange(0f, Mathf.Tau);
            var points = new Vector2[7];
            for (int p = 0; p < points.Length; p++)
            {
                float t = p / (float)(points.Length - 1);
                float x = seamX - width * 0.5f + width * t;
                float wobble = Mathf.Sin(t * Mathf.Tau * rng.RandfRange(0.65f, 1.2f) + phase) * amplitude;
                points[p] = new Vector2(x, y + wobble);
            }

            map.AddChild(new Line2D
            {
                Name = "SeamCurrent",
                Points = points,
                Width = rng.RandfRange(4f, 8f),
                DefaultColor = lineColor,
                Antialiased = true,
                ZIndex = zIndex,
            });
        }
    }
}

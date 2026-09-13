using Godot;

namespace ShallowSeaDream;

/// res://scripts/Palette.cs
/// Single source of truth for the game's colour identity (ported verbatim from
/// the original scripts/palette.gd). WARNING_AMBER is reserved for player-damage only.
public static class Palette
{
    public static readonly Color DeepSea = new(0.04f, 0.12f, 0.18f);
    public static readonly Color SurfaceLight = new(0.56f, 0.82f, 0.96f);
    public static readonly Color CoastalCyan = new(0.52f, 0.94f, 1.0f);
    public static readonly Color PollutedTeal = new(0.42f, 0.86f, 0.78f);
    public static readonly Color PollutedTealBright = new(0.56f, 0.92f, 0.72f);

    /// Player-damage only — the single warm accent. Do not reuse for enemies.
    public static readonly Color WarningAmber = new(0.98f, 0.74f, 0.32f);

    /// Atmosphere around a chapter guardian, keyed to that chapter's pollution.
    public static Color ArenaTint(int chapter) => chapter switch
    {
        2 => new Color(0.58f, 0.72f, 0.18f),   // chemical
        3 => new Color(0.82f, 0.36f, 0.12f),   // oil
        _ => new Color(0.30f, 0.52f, 0.72f),   // plastic
    };

    public static readonly Color ElementWater = new(0.30f, 0.70f, 1.0f);
    public static readonly Color ElementIce = new(0.70f, 0.92f, 1.0f);
    public static readonly Color ElementElectric = new(0.90f, 1.0f, 0.30f);

    public static readonly Color ElementWaterImpact = new(0.55f, 0.85f, 1.0f);
    public static readonly Color ElementIceImpact = new(0.85f, 0.97f, 1.0f);
    public static readonly Color ElementElectricImpact = new(0.97f, 1.0f, 0.60f);

    /// Returns the accent colour for a given elemental form.
    public static Color ForForm(ElementForm form) => form switch
    {
        ElementForm.Water => ElementWater,
        ElementForm.Ice => ElementIce,
        ElementForm.Electric => ElementElectric,
        _ => SurfaceLight,
    };
}

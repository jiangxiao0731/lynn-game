using Godot;

namespace ShallowSeaDream;

/// res://scripts/AssetLoader.cs
/// Graceful asset loading: code references the FINAL asset paths selected from the
/// owner's supplied artwork folders. The game keeps running with procedural fallbacks
/// if an optional painting is unavailable. Every loader returns
/// null instead of throwing when a file is missing, so callers can fall back.
public static class AssetLoader
{
    /// Load a Texture2D from res:// if it exists, else null (caller draws a placeholder).
    public static Texture2D? Texture(string resPath)
    {
        if (string.IsNullOrEmpty(resPath)) return null;
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }

    /// Load any resource (SpriteFrames, AudioStream, Shader, ...) or null if absent.
    public static T? Resource<T>(string resPath) where T : Resource
    {
        if (string.IsNullOrEmpty(resPath)) return null;
        return ResourceLoader.Exists(resPath) ? GD.Load<T>(resPath) : null;
    }

    /// True when the file is present in the project / export.
    public static bool Has(string resPath) => !string.IsNullOrEmpty(resPath) && ResourceLoader.Exists(resPath);

    // --- Canonical runtime paths (source provenance is mirrored in ASSET_MANIFEST.md) ---

    public static string FormSheet(ElementForm form) => form switch
    {
        ElementForm.Water => "res://assets/sprites/jellyfish_water.png",
        ElementForm.Ice => "res://assets/sprites/jellyfish_ice.png",
        ElementForm.Electric => "res://assets/sprites/jellyfish_electric.png",
        _ => "res://assets/sprites/jellyfish_base.png",
    };

    /// The selected source-folder sheets are kept intact. Player animation softly
    /// keys their presentation paper at runtime without rewriting the paintings.
    public static string RawFormSheet(ElementForm form) => form switch
    {
        ElementForm.Water => "res://assets/sprites/_raw/jellyfish_water.png",
        ElementForm.Ice => "res://assets/sprites/_raw/jellyfish_ice.png",
        ElementForm.Electric => "res://assets/sprites/_raw/jellyfish_electric.png",
        _ => "res://assets/sprites/_raw/jellyfish_base.png",
    };

    public const string BossSheet = "res://assets/sprites/brood_mother.png";
    public const string WaterAttackSheet = "res://assets/sprites/water_attack.png";
    public const string ShardIcon = "res://assets/img/water_shard.png";
    public const string MapBackground = "res://assets/img/maze_map.png";
    public const string TitleBackground = "res://assets/img/title_bg.png";
    public const string WallTile = "res://assets/img/wall_tile.png";

    public static string Invader(int index) => $"res://assets/sprites/invader_{index}.png";

    public static string NpcPortrait(string id) => $"res://assets/img/npc_{id}.png";

    // --- Story-first overhaul assets (graceful fallback to procedural placeholders) ---
    public const string MemoryIcon = "res://assets/img/icon_memory.png";
    public const string NoteIcon = "res://assets/img/icon_note.png";

    /// Background for a narrative zone ("浅滩" / "沉积带" / "巢母深处").
    /// Chapter one owns these three zones, so it reads its own painted panels.
    public static string ZoneBackground(string zone) => zone switch
    {
        NarrativeData.ZoneSediment => ChapterBackground(1, 1),
        NarrativeData.ZoneDepths => ChapterBackground(1, 2),
        _ => ChapterBackground(1, 0),
    };

    // --- Per-chapter painted set: three panels laid head to tail per chapter ---

    /// Left-to-right order the three painted panels are laid in, per chapter.
    ///
    /// The paintings were not delivered in the order they join up: each one's left and
    /// right edges only line up with particular neighbours, so laying them 1-2-3 left a
    /// visible break at each seam. These sequences were picked by measuring how closely
    /// one panel's right edge matches the next panel's left edge, and they cut the
    /// mismatch roughly in half (chapter two by about 8x).
    private static readonly int[][] PanelOrder =
    {
        new[] { 2, 3, 1 },   // chapter one
        new[] { 3, 2, 1 },   // chapter two
        new[] { 1, 2, 3 },   // chapter three — already delivered in joining order
    };

    /// One of the three horizontal panels tiled into a chapter's continuous backdrop.
    /// `index` is the slot, 0..2 left to right, and wraps outside that range; the file
    /// it resolves to comes from PanelOrder.
    public static string ChapterBackground(int chapter, int index)
    {
        int ch = Mathf.Clamp(chapter, 1, 3);
        int file = PanelOrder[ch - 1][Mathf.PosMod(index, 3)];
        return $"res://assets/img/ch{ch}_bg_{file}.jpg";
    }

    /// The single guardian that closes a chapter.
    public static string ChapterBoss(int chapter)
        => $"res://assets/sprites/boss_ch{Mathf.Clamp(chapter, 1, 3)}.png";

    /// Portrait shown when a chapter guardian speaks.
    public static string ChapterBossPortrait(int chapter)
        => NpcPortrait($"boss{Mathf.Clamp(chapter, 1, 3)}");

    /// The one element collected in a chapter (Water / Ice / Electric).
    public static string ChapterElement(int chapter)
        => $"res://assets/sprites/element_ch{Mathf.Clamp(chapter, 1, 3)}.png";
}

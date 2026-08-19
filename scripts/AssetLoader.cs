using Godot;

namespace ShallowSeaDream;

/// res://scripts/AssetLoader.cs
/// Graceful asset loading: code references the FINAL asset paths
/// (res://assets/img/..., res://assets/sprites/...) but the game keeps running
/// with procedural placeholders until the art is generated. Every loader returns
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

    // --- Canonical final asset paths (single source of truth, mirrored in ASSET_MANIFEST.md) ---

    public static string FormSheet(ElementForm form) => form switch
    {
        ElementForm.Water => "res://assets/sprites/jellyfish_water.png",
        ElementForm.Ice => "res://assets/sprites/jellyfish_ice.png",
        ElementForm.Electric => "res://assets/sprites/jellyfish_electric.png",
        _ => "res://assets/sprites/jellyfish_base.png",
    };

    /// Source sheets are intentionally kept because the earlier background-removal
    /// pass erased most of the translucent jellyfish. Player animation now keys the
    /// dark teal paper out at runtime, preserving the existing painted frames.
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

    /// Background for a narrative zone ("浅滩" / "沉积带" / "巢母深处"); null when absent.
    public static string ZoneBackground(string zone) => zone switch
    {
        "沉积带" => "res://assets/img/zone_sediment.png",
        "巢母深处" => "res://assets/img/zone_depths.png",
        _ => "res://assets/img/zone_shallows.png",
    };
}

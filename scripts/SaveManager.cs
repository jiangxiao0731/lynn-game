using Godot;

namespace ShallowSeaDream;

/// res://scripts/SaveManager.cs
/// Autoload save service (ported from SaveManager.gd). JSON at user://save.json.
/// Persisted fields (see DESIGN_BRIEF §10):
///   version, saved_at (ISO-8601), current_form, current_health,
///   element_charges{water,ice,electric}, objective_stage,
///   next_level_element_unlocked, first_boss_defeated, first_water_collected.
public partial class SaveManager : Node
{
    [Signal] public delegate void SaveCompletedEventHandler();
    [Signal] public delegate void LoadCompletedEventHandler();

    public const int SaveVersion = 2;
    public const string SavePath = "user://save.json";

    public static SaveManager Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public bool HasSave()
    {
        return FileAccess.FileExists(SavePath);
    }

    /// Serialise the supplied progress snapshot to user://save.json (see §10).
    public void SaveState(SaveState state)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["version"] = SaveVersion,
            ["saved_at"] = Time.GetDatetimeStringFromSystem(true),
            ["current_form"] = (int)state.CurrentForm,
            ["current_health"] = state.CurrentHealth,
            ["element_charges"] = new Godot.Collections.Dictionary
            {
                ["water"] = state.WaterCharges,
                ["ice"] = state.IceCharges,
                ["electric"] = state.ElectricCharges,
            },
            ["objective_stage"] = (int)state.ObjectiveStage,
            ["current_level"] = state.CurrentLevel,
            ["collected_count"] = state.CollectedCount,
            ["next_level_element_unlocked"] = state.NextLevelElementUnlocked.HasValue
                ? FormKey(state.NextLevelElementUnlocked.Value) : "",
            ["first_boss_defeated"] = state.FirstBossDefeated,
            ["first_water_collected"] = state.FirstWaterCollected,
        };

        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning($"SaveManager: cannot open {SavePath} for write");
            return;
        }
        f.StoreString(Json.Stringify(dict, "  "));
        EmitSignal(SignalName.SaveCompleted);
    }

    /// Commit a chapter transition. Mid-chapter topology is intentionally not
    /// serialized; Continue always resumes at the beginning of the last entered
    /// chapter, so gates, unique pickups and guardian state cannot disagree.
    public void SaveCampaignBoundary(int nextLevel)
    {
        var current = LoadState() ?? new SaveState(SaveVersion, "", ElementForm.Base,
            GameConstants.MaxHealth, 0, 0, 0, ObjectiveStage.FindNpc, null, false, false);
        SaveState(current with
        {
            Version = SaveVersion,
            SavedAt = Time.GetDatetimeStringFromSystem(true),
            CurrentLevel = Mathf.Clamp(nextLevel, 1, 3),
            ObjectiveStage = ObjectiveStage.FindNpc,
            CollectedCount = 0,
        });
    }

    /// Read user://save.json; returns null when absent or invalid.
    public SaveState? LoadState()
    {
        if (!HasSave()) return null;
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (f == null) return null;

        var parsed = Json.ParseString(f.GetAsText());
        if (parsed.VariantType != Variant.Type.Dictionary) return null;
        var d = parsed.AsGodotDictionary();

        int maxHp = GameConstants.MaxHealth;
        int hp = Mathf.Clamp(GetInt(d, "current_health", maxHp), 1, maxHp);
        var charges = d.TryGetValue("element_charges", out var c) && c.VariantType == Variant.Type.Dictionary
            ? c.AsGodotDictionary() : new Godot.Collections.Dictionary();

        var state = new SaveState(
            Version: GetInt(d, "version", SaveVersion),
            SavedAt: d.TryGetValue("saved_at", out var sa) ? sa.AsString() : "",
            CurrentForm: (ElementForm)GetInt(d, "current_form", 0),
            CurrentHealth: hp,
            WaterCharges: GetInt(charges, "water", 0),
            IceCharges: GetInt(charges, "ice", 0),
            ElectricCharges: GetInt(charges, "electric", 0),
            ObjectiveStage: (ObjectiveStage)GetInt(d, "objective_stage", 0),
            NextLevelElementUnlocked: ParseFormKey(d.TryGetValue("next_level_element_unlocked", out var nl) ? nl.AsString() : ""),
            FirstBossDefeated: GetBool(d, "first_boss_defeated"),
            FirstWaterCollected: GetBool(d, "first_water_collected"),
            CurrentLevel: Mathf.Clamp(GetInt(d, "current_level", 1), 1, 3),
            CollectedCount: Mathf.Max(0, GetInt(d, "collected_count", 0)));

        EmitSignal(SignalName.LoadCompleted);
        return state;
    }

    public void Reset()
    {
        if (HasSave())
        {
            string global = ProjectSettings.GlobalizePath(SavePath);
            DirAccess.RemoveAbsolute(global);
        }
    }

    private static int GetInt(Godot.Collections.Dictionary d, string key, int fallback) =>
        d.TryGetValue(key, out var v) ? (int)v.AsDouble() : fallback;

    private static bool GetBool(Godot.Collections.Dictionary d, string key) =>
        d.TryGetValue(key, out var v) && v.AsBool();

    private static string FormKey(ElementForm form) => form switch
    {
        ElementForm.Ice => "ice",
        ElementForm.Electric => "electric",
        ElementForm.Water => "water",
        _ => "",
    };

    private static ElementForm? ParseFormKey(string key) => key switch
    {
        "ice" => ElementForm.Ice,
        "electric" => ElementForm.Electric,
        "water" => ElementForm.Water,
        _ => null,
    };
}

/// Immutable snapshot of persisted progress.
public sealed record SaveState(
    int Version,
    string SavedAt,
    ElementForm CurrentForm,
    int CurrentHealth,
    int WaterCharges,
    int IceCharges,
    int ElectricCharges,
    ObjectiveStage ObjectiveStage,
    ElementForm? NextLevelElementUnlocked,
    bool FirstBossDefeated,
    bool FirstWaterCollected,
    int CurrentLevel = 1,
    int CollectedCount = 0);

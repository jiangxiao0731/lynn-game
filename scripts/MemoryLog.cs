using Godot;
using System.Collections.Generic;

namespace ShallowSeaDream;

/// res://scripts/MemoryLog.cs
/// Autoload journal service backing the 「记忆日志 / 图鉴」 (item 5). Records which
/// memory vignettes were unlocked, which NPC conversations were seen, which monster
/// codex entries and lore notes were found, plus branching story flags (item 2/6).
/// Persisted to user://log.json (parallel to SaveManager's save.json) so the journal
/// survives across sessions. Sets are de-duplicated and insertion-ordered for display.
public partial class MemoryLog : Node
{
    public const int LogVersion = 1;
    public const string LogPath = "user://log.json";

    public static MemoryLog Instance { get; private set; } = null!;

    private readonly List<string> _memories = new();
    private readonly List<string> _conversations = new();
    private readonly List<string> _codex = new();
    private readonly List<string> _notes = new();
    private readonly HashSet<string> _flags = new();
    private readonly int[] _pollutionCollected = new int[3];
    private readonly bool[] _guardiansRestored = new bool[3];

    public IReadOnlyList<string> Memories => _memories;
    public IReadOnlyList<string> Conversations => _conversations;
    public IReadOnlyList<string> Codex => _codex;
    public IReadOnlyList<string> Notes => _notes;
    public int WaterCollected => _pollutionCollected[0];
    public int IcePollutionCollected => _pollutionCollected[1];
    public int OilHeatCollected => _pollutionCollected[2];
    public int TotalPollutionCollected => WaterCollected + IcePollutionCollected + OilHeatCollected;
    public int GuardiansRestored => System.Array.FindAll(_guardiansRestored, value => value).Length;

    public override void _EnterTree()
    {
        Instance = this;
        Load();
        ReconnectEvents();
    }

    public void ReconnectEvents()
    {
        if (Events.Instance != null)
        {
            Events.Instance.ElementPickedUp -= OnElementPickedUp;
            Events.Instance.BossDefeated -= OnBossDefeated;
            Events.Instance.ElementPickedUp += OnElementPickedUp;
            Events.Instance.BossDefeated += OnBossDefeated;
        }
    }

    private void OnElementPickedUp(int form)
    {
        int index = Mathf.Clamp((int)(ElementForm)form - 1, 0, 2);
        _pollutionCollected[index]++;
        Save();
    }

    private void OnBossDefeated()
    {
        _guardiansRestored[Mathf.Clamp(ChapterRuntime.CurrentChapter - 1, 0, 2)] = true;
        Save();
    }

    public bool HasFlag(string flag) => _flags.Contains(flag);

    public void SetFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;
        if (_flags.Add(flag)) Save();
    }

    public void UnlockMemory(string id) => AddUnique(_memories, id);
    public void RecordConversation(string timelineId) => AddUnique(_conversations, timelineId);
    public void UnlockCodex(string id) => AddUnique(_codex, id);
    public void RecordNote(string id) => AddUnique(_notes, id);

    private void AddUnique(List<string> list, string id)
    {
        if (string.IsNullOrEmpty(id) || list.Contains(id)) return;
        list.Add(id);
        Save();
    }

    public void Reset()
    {
        _memories.Clear();
        _conversations.Clear();
        _codex.Clear();
        _notes.Clear();
        _flags.Clear();
        System.Array.Clear(_pollutionCollected, 0, _pollutionCollected.Length);
        System.Array.Clear(_guardiansRestored, 0, _guardiansRestored.Length);
        if (FileAccess.FileExists(LogPath))
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(LogPath));
    }

    private void Save()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["version"] = LogVersion,
            ["memories"] = new Godot.Collections.Array(_memories.ConvertAll(s => (Variant)s)),
            ["conversations"] = new Godot.Collections.Array(_conversations.ConvertAll(s => (Variant)s)),
            ["codex"] = new Godot.Collections.Array(_codex.ConvertAll(s => (Variant)s)),
            ["notes"] = new Godot.Collections.Array(_notes.ConvertAll(s => (Variant)s)),
            ["flags"] = new Godot.Collections.Array(new List<string>(_flags).ConvertAll(s => (Variant)s)),
            ["pollution_collected"] = new Godot.Collections.Array
            {
                _pollutionCollected[0], _pollutionCollected[1], _pollutionCollected[2],
            },
            ["guardians_restored"] = new Godot.Collections.Array
            {
                _guardiansRestored[0], _guardiansRestored[1], _guardiansRestored[2],
            },
        };
        using var f = FileAccess.Open(LogPath, FileAccess.ModeFlags.Write);
        if (f == null) { GD.PushWarning($"MemoryLog: cannot open {LogPath}"); return; }
        f.StoreString(Json.Stringify(dict, "  "));
    }

    private void Load()
    {
        if (!FileAccess.FileExists(LogPath)) return;
        using var f = FileAccess.Open(LogPath, FileAccess.ModeFlags.Read);
        if (f == null) return;
        var parsed = Json.ParseString(f.GetAsText());
        if (parsed.VariantType != Variant.Type.Dictionary) return;
        var d = parsed.AsGodotDictionary();
        ReadInto(d, "memories", _memories);
        ReadInto(d, "conversations", _conversations);
        ReadInto(d, "codex", _codex);
        ReadInto(d, "notes", _notes);
        var flags = new List<string>();
        ReadInto(d, "flags", flags);
        foreach (var s in flags) _flags.Add(s);
        if (d.TryGetValue("pollution_collected", out var collected) && collected.VariantType == Variant.Type.Array)
        {
            var values = collected.AsGodotArray();
            for (int i = 0; i < Mathf.Min(3, values.Count); i++)
                _pollutionCollected[i] = Mathf.Max(0, (int)values[i].AsDouble());
        }
        if (d.TryGetValue("guardians_restored", out var restored) && restored.VariantType == Variant.Type.Array)
        {
            var values = restored.AsGodotArray();
            for (int i = 0; i < Mathf.Min(3, values.Count); i++)
                _guardiansRestored[i] = values[i].AsBool();
        }
    }

    private static void ReadInto(Godot.Collections.Dictionary d, string key, List<string> target)
    {
        if (!d.TryGetValue(key, out var v) || v.VariantType != Variant.Type.Array) return;
        foreach (var item in v.AsGodotArray())
            target.Add(item.AsString());
    }
}

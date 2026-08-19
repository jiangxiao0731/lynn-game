using Godot;

namespace ShallowSeaDream;

/// res://scripts/AudioManager.cs
/// Autoload audio service (ported from scripts/AudioManager.gd).
/// Buses: Master, Music, SFX, Dialogue. Music loops; SFX uses a small player pool.
public partial class AudioManager : Node
{
    [Signal] public delegate void DialogueLineStartedEventHandler(string speaker, string lineText);

    [Export] public float MasterVolume = 0.9f;
    [Export] public float MusicVolume = 0.32f;
    [Export] public float SfxVolume = 1.0f;
    [Export] public float DialogueVolume = 0.9f;

    public static AudioManager Instance { get; private set; } = null!;

    private const int SfxPoolSize = 6;

    private AudioStreamPlayer? _musicPlayer;
    private AudioStreamPlayer? _dialoguePlayer;
    private readonly AudioStreamPlayer[] _sfxPool = new AudioStreamPlayer[SfxPoolSize];
    private int _sfxCursor;
    private readonly System.Collections.Generic.Dictionary<string, AudioStream?> _cache = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        _musicPlayer = new AudioStreamPlayer { Name = "Music", VolumeDb = LinearToDb(MasterVolume * MusicVolume) };
        AddChild(_musicPlayer);
        _dialoguePlayer = new AudioStreamPlayer { Name = "Dialogue", VolumeDb = LinearToDb(MasterVolume * DialogueVolume) };
        AddChild(_dialoguePlayer);
        for (int i = 0; i < SfxPoolSize; i++)
        {
            var p = new AudioStreamPlayer { Name = $"Sfx{i}", VolumeDb = LinearToDb(MasterVolume * SfxVolume) };
            AddChild(p);
            _sfxPool[i] = p;
        }
    }

    private static float LinearToDb(float linear) =>
        linear <= 0.0001f ? -80f : Mathf.LinearToDb(linear);

    private AudioStream? StreamFor(string key)
    {
        if (_cache.TryGetValue(key, out var cached)) return cached;
        var stream = AssetLoader.Resource<AudioStream>($"res://assets/audio/{key}.ogg");
        _cache[key] = stream; // null caches the miss so we don't retry every call
        return stream;
    }

    /// Play a looping music stream on the Music bus (graceful no-op if missing).
    public void PlayMusic(string streamPath)
    {
        if (_musicPlayer == null) return;
        var stream = StreamFor(streamPath);
        if (stream == null) return;
        if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
        _musicPlayer.Stream = stream;
        _musicPlayer.Play();
    }

    public void StopMusic() => _musicPlayer?.Stop();

    /// Play a one-shot SFX from the pool. Key matches the original .ogg filenames.
    public void PlaySfx(string sfxKey)
    {
        var stream = StreamFor(sfxKey);
        if (stream == null) return;
        var player = _sfxPool[_sfxCursor];
        _sfxCursor = (_sfxCursor + 1) % SfxPoolSize;
        player.Stream = stream;
        player.Play();
    }

    /// Emits DialogueLineStarted for caption / accessibility hooks.
    public void PlayDialogue(AudioStream? stream, string speaker, string lineText)
    {
        EmitSignal(SignalName.DialogueLineStarted, speaker, lineText);
        if (stream != null && _dialoguePlayer != null)
        {
            _dialoguePlayer.Stream = stream;
            _dialoguePlayer.Play();
        }
    }
}

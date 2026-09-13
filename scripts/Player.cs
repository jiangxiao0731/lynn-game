using Godot;

namespace ShallowSeaDream;

/// res://scripts/Player.cs
/// Movement + form/health state for 微光 / Shimmer. Part of the god-class split:
/// owns ONLY movement, facing, form display and health. Skills live in SkillSystem,
/// objectives in ObjectiveManager. Communicates outward via the Events bus.
public partial class Player : CharacterBody2D
{
    [Signal] public delegate void FormChangedEventHandler(int form);

    [Export] public float Speed = GameConstants.Speed;
    /// Shift-to-sprint factor; tuned so a full chapter crossing stays under a minute.
    [Export] public float SprintMultiplier = 5.2f;
    /// Contractions per second while swimming.
    [Export] public float PulseRate = 1.45f;
    /// Speed kept during the coast, as a fraction of full thrust.
    [Export] public float PulseFloor = 0.22f;

    private float _pulse;
    [Export] public int MaxHealthValue = GameConstants.MaxHealth;

    public int CurrentHealth { get; private set; } = GameConstants.MaxHealth;
    public ElementForm CurrentForm { get; private set; } = ElementForm.Base;

    private readonly System.Collections.Generic.Dictionary<ElementForm, AnimatedSprite2D> _sprites = new();
    private Camera2D? _camera;
    private float _flashTime;
    private float _shakeTime;
    private bool _dialogueActive;

    // Item 4 — soft death: last safe spot the tide carries the player back to.
    private Vector2 _safePoint;
    private bool _respawning;

    private static readonly (ElementForm Form, string Node)[] SpriteNodes =
    {
        (ElementForm.Base, "AnimatedSprite2D"),
        (ElementForm.Water, "WaterSprite2D"),
        (ElementForm.Ice, "IceSprite2D"),
        (ElementForm.Electric, "ElectricSprite2D"),
    };

    /// Target size of the full animation cell. The painted jellyfish occupies only
    /// about half of each cell, so 176px produces a protagonist silhouette around
    /// 90–110px tall in the 1080p game viewport. The intentionally smaller 36px
    /// collision radius remains forgiving even though the character now reads large.
    private const float DisplaySize = 176f;

    /// The resting modulate for a form: white for Base, a gentle half-tint otherwise.
    /// Keeps the cut-out art bright and legible instead of washed dark.
    private static Color FormModulate(ElementForm form) => form == ElementForm.Base
        ? Colors.White
        : Colors.White.Lerp(Palette.ForForm(form), 0.5f);

    public override void _Ready()
    {
        AddToGroup("player");
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        _safePoint = GlobalPosition;

        var bus = Events.Instance;
        if (bus != null)
        {
            bus.DialogueStarted += _ => _dialogueActive = true;
            bus.DialogueFinished += _ => _dialogueActive = false;
        }

        foreach (var (form, node) in SpriteNodes)
        {
            var sprite = GetNodeOrNull<AnimatedSprite2D>(node);
            if (sprite == null) continue;
            sprite.SpriteFrames = PlaceholderArt.FormFrames(form);
            // Scale the (large) sliced cell down to a sensible on-screen size so it
            // doesn't fill the viewport.
            var frames = sprite.SpriteFrames;
            var names = frames.GetAnimationNames();
            var firstName = names.Length > 0 ? names[0] : "Idle";
            var frame0 = frames.GetFrameCount(firstName) > 0 ? frames.GetFrameTexture(firstName, 0) : null;
            if (frame0 != null)
            {
                float src = Mathf.Max(frame0.GetWidth(), frame0.GetHeight());
                if (src > 0f) sprite.Scale = new Vector2(DisplaySize / src, DisplaySize / src);
            }
            // Base form keeps full brightness (white); elemental forms get a gentle
            // tint at half strength so the art stays legible, not washed dark.
            sprite.Modulate = FormModulate(form);
            sprite.Play("Idle");
            _sprites[form] = sprite;
        }
        ShowActiveForm();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_dialogueActive)
        {
            Velocity = Vector2.Zero;
            UpdateDamageFeedback((float)delta);
            return;
        }
        var dir = Input.GetVector("Move_Left", "Move_Right", "Move_Up", "Move_Down");
        // Hold Shift to sprint. The chapters are ~17000px across, so crossing one at
        // the base pace is a long swim; sprint keeps the scale without the slog.
        bool sprinting = Input.IsKeyPressed(Key.Shift);

        // Jellyfish propulsion: a hard contraction, then a coast. The cycle only runs
        // while a direction is held, so releasing the keys stops mid-glide instead of
        // snapping. Sprinting beats faster rather than simply moving faster.
        bool moving = dir.LengthSquared() > 0.01f;
        float rate = sprinting ? PulseRate * 1.7f : PulseRate;
        if (moving) _pulse += (float)delta * rate;
        else _pulse = 0f;

        // 0..1 saw, shaped so the first third is the push and the rest is the drift.
        float phase = _pulse - Mathf.Floor(_pulse);
        float burst = phase < 0.34f
            ? Mathf.Sin(phase / 0.34f * Mathf.Pi)          // contraction
            : 0.18f * (1f - (phase - 0.34f) / 0.66f);      // decaying coast
        float thrust = Mathf.Lerp(PulseFloor, 1f, burst);

        Velocity = dir * Speed * (sprinting ? SprintMultiplier : 1f) * thrust;
        MoveAndSlide();

        if (_sprites.TryGetValue(CurrentForm, out var active))
        {
            string anim = moving ? "Walk" : "Idle";
            if (active.Animation != anim) active.Play(anim);
            if (Mathf.Abs(dir.X) > 0.01f) active.FlipH = dir.X < 0f;
        }

        UpdateDamageFeedback((float)delta);
    }

    private void UpdateDamageFeedback(float delta)
    {
        if (_flashTime > 0f)
        {
            _flashTime = Mathf.Max(0f, _flashTime - delta);
            if (_sprites.TryGetValue(CurrentForm, out var s))
            {
                float t = _flashTime / 0.25f;
                s.Modulate = FormModulate(CurrentForm).Lerp(Palette.WarningAmber, t);
            }
        }
        if (_camera != null && _shakeTime > 0f)
        {
            _shakeTime = Mathf.Max(0f, _shakeTime - delta);
            float mag = 8f * (_shakeTime / 0.3f);
            _camera.Offset = new Vector2(
                (float)GD.RandRange(-mag, mag), (float)GD.RandRange(-mag, mag));
            if (_shakeTime <= 0f) _camera.Offset = Vector2.Zero;
        }
    }

    private void ShowActiveForm()
    {
        foreach (var (form, sprite) in _sprites)
            sprite.Visible = form == CurrentForm;
    }

    /// Switch active form, update the visible sprite + modulate, emit FormChanged.
    public void SetForm(ElementForm form)
    {
        CurrentForm = form;
        ShowActiveForm();
        if (_sprites.TryGetValue(form, out var s))
        {
            s.Modulate = FormModulate(form);
            s.Play("Idle");
        }
        EmitSignal(SignalName.FormChanged, (int)form);
        Events.Instance?.EmitSignal(Events.SignalName.PlayerFormChanged, (int)form);
    }

    /// Mark a safe spot to return to on soft-respawn (e.g. on entering a new zone /
    /// beginning a beat). Item 4: keeps story flow intact, no game-over.
    public void SetSafePoint(Vector2 worldPos) => _safePoint = worldPos;

    /// Apply pollution damage (boss only). Uses WARNING_AMBER flash + camera shake.
    public void TakeDamage(int amount)
    {
        if (_respawning || CurrentHealth <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        _flashTime = 0.25f;
        _shakeTime = 0.3f;
        Events.Instance?.EmitSignal(Events.SignalName.PlayerDamaged, amount);
        Events.Instance?.EmitSignal(Events.SignalName.PlayerHealthChanged, CurrentHealth, MaxHealthValue);
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
            string.Format(GameStrings.Tr("DAMAGE_RECEIVED_TEMPLATE"), amount));
        AudioManager.Instance?.PlaySfx("player_hit");
        if (CurrentHealth == 0)
            SoftRespawn();
    }

    /// Item 4 — eco-fable soft death: fade out, restore full shimmer, and let the tide
    /// carry the player back to the last safe point. No FailurePanel, no run-ending.
    private void SoftRespawn()
    {
        if (_respawning) return;
        _respawning = true;
        Events.Instance?.EmitSignal(Events.SignalName.PlayerDefeated); // gentle hint only
        Events.Instance?.EmitSignal(Events.SignalName.StatusHint, GameStrings.Tr("SOFT_RESPAWN"));
        AudioManager.Instance?.PlaySfx("player_defeat");

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.15f, 0.4f);
        tween.TweenCallback(Callable.From(() =>
        {
            GlobalPosition = _safePoint;
            CurrentHealth = MaxHealthValue;
            Events.Instance?.EmitSignal(Events.SignalName.PlayerHealthChanged, CurrentHealth, MaxHealthValue);
        }));
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.4f);
        tween.TweenCallback(Callable.From(() => _respawning = false));
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealthValue, CurrentHealth + amount);
        Events.Instance?.EmitSignal(Events.SignalName.PlayerHealthChanged, CurrentHealth, MaxHealthValue);
    }

    /// Retract the current form back to base (store_element / key R).
    public void StoreForm()
    {
        SetForm(ElementForm.Base);
        AudioManager.Instance?.PlaySfx("store_form");
    }

    /// Re-apply persisted health + form on resume (called by GameSceneController).
    public void RestoreState(int health, ElementForm form)
    {
        CurrentHealth = Mathf.Clamp(health, 1, MaxHealthValue);
        SetForm(form);
        Events.Instance?.EmitSignal(Events.SignalName.PlayerHealthChanged, CurrentHealth, MaxHealthValue);
    }

    /// Broadcast initial HP so the HUD ring/bar populate on level start.
    public void BroadcastHealth() =>
        Events.Instance?.EmitSignal(Events.SignalName.PlayerHealthChanged, CurrentHealth, MaxHealthValue);
}

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
    /// Shift-to-sprint factor. Base speed is already the old sprint pace; Shift is a
    /// faster traversal mode for long chapter crossings.
    [Export] public float SprintMultiplier = 2.0f;
    /// Contractions per second while swimming.
    [Export] public float PulseRate = 1.45f;
    /// Speed kept during the coast, as a fraction of full thrust.
    [Export] public float PulseFloor = 0.22f;

    private float _pulse;
    [Export] public int MaxHealthValue = GameConstants.MaxHealth;

    public int CurrentHealth { get; private set; } = GameConstants.MaxHealth;
    public ElementForm CurrentForm { get; private set; } = ElementForm.Base;

    private readonly System.Collections.Generic.Dictionary<ElementForm, AnimatedSprite2D> _sprites = new();
    private readonly System.Collections.Generic.Dictionary<ElementForm, Vector2> _restScales = new();
    private readonly System.Collections.Generic.Dictionary<ElementForm, Vector2> _restPositions = new();
    private readonly System.Collections.Generic.Dictionary<ElementForm, Tween> _idleTweens = new();
    private Camera2D? _camera;
    private float _flashTime;
    private float _shakeTime;
    private bool _dialogueActive;
    private Sprite2D? _damageBurst;
    private Sprite2D? _cleanupBurst;
    private Tween? _attackTween;
    private Tween? _attackBurstTween;
    private ElementForm? _attackForm;

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
            _restScales[form] = sprite.Scale;
            _restPositions[form] = sprite.Position;
            StartNaturalIdle(sprite, form);
        }
        BuildDamageBurst();
        BuildCleanupBurst();
        ShowActiveForm();
    }

    private void BuildDamageBurst()
    {
        _damageBurst = new Sprite2D
        {
            Name = "DamageBurst",
            Texture = PlaceholderArt.SoftGlow(new Color(1f, 0.26f, 0.18f, 0.92f), 256),
            Scale = Vector2.One * 0.18f,
            Modulate = new Color(1f, 1f, 1f, 0f),
            ZIndex = 100,
        };
        AddChild(_damageBurst);
    }

    private void BuildCleanupBurst()
    {
        _cleanupBurst = new Sprite2D
        {
            Name = "CleanupBurst",
            Texture = PlaceholderArt.SoftGlow(Colors.White, 256),
            Scale = Vector2.One * 0.12f,
            Modulate = new Color(1f, 1f, 1f, 0f),
            ZIndex = 99,
        };
        AddChild(_cleanupBurst);
    }

    private void StartNaturalIdle(AnimatedSprite2D sprite, ElementForm form)
    {
        if (!_restPositions.TryGetValue(form, out var restPosition)) restPosition = sprite.Position;
        if (!_restScales.TryGetValue(form, out var restScale)) restScale = sprite.Scale;
        StopNaturalIdle(form);
        sprite.Position = restPosition;
        sprite.Scale = restScale;
        sprite.Rotation = 0f;
        float drift = form switch
        {
            ElementForm.Ice => 4.5f,
            ElementForm.Electric => 5.2f,
            _ => 6.0f,
        };
        float seconds = form switch
        {
            ElementForm.Ice => 2.9f,
            ElementForm.Electric => 2.15f,
            _ => 2.55f,
        };
        float delay = ((int)form + 1) * 0.13f;

        var upScale = new Vector2(restScale.X * 1.018f, restScale.Y * 0.992f);
        var downScale = new Vector2(restScale.X * 0.994f, restScale.Y * 1.014f);
        var tween = sprite.CreateTween().SetLoops();
        tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenInterval(delay);
        tween.TweenProperty(sprite, "position:y", restPosition.Y - drift, seconds * 0.5f);
        tween.Parallel().TweenProperty(sprite, "scale", upScale, seconds * 0.5f);
        tween.TweenProperty(sprite, "position:y", restPosition.Y + drift * 0.65f, seconds);
        tween.Parallel().TweenProperty(sprite, "scale", downScale, seconds);
        tween.TweenProperty(sprite, "position:y", restPosition.Y, seconds * 0.5f);
        tween.Parallel().TweenProperty(sprite, "scale", restScale, seconds * 0.5f);
        _idleTweens[form] = tween;
    }

    private void StopNaturalIdle(ElementForm form)
    {
        if (!_idleTweens.TryGetValue(form, out var tween)) return;
        tween.Kill();
        _idleTweens.Remove(form);
    }

    private void RestoreFormPose(ElementForm form)
    {
        if (!_sprites.TryGetValue(form, out var sprite)) return;
        if (_restPositions.TryGetValue(form, out var position)) sprite.Position = position;
        if (_restScales.TryGetValue(form, out var scale)) sprite.Scale = scale;
        sprite.Rotation = 0f;
        sprite.Modulate = FormModulate(form);
    }

    private void CancelCleanupAttack()
    {
        _attackTween?.Kill();
        _attackBurstTween?.Kill();
        _attackTween = null;
        _attackBurstTween = null;
        if (_attackForm is not { } form) return;
        RestoreFormPose(form);
        if (_sprites.TryGetValue(form, out var sprite)) StartNaturalIdle(sprite, form);
        _attackForm = null;
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

    /// Face a conversation partner without changing position or movement state.
    public void FaceTowards(Vector2 worldPoint)
    {
        if (_sprites.TryGetValue(CurrentForm, out var active) &&
            Mathf.Abs(worldPoint.X - GlobalPosition.X) > 1f)
            active.FlipH = worldPoint.X < GlobalPosition.X;
    }

    /// Switch active form, update the visible sprite + modulate, emit FormChanged.
    public void SetForm(ElementForm form)
    {
        if (_attackForm is { } attacking && attacking != form)
            CancelCleanupAttack();
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

    /// A readable purification gesture shared by all three chapter skills. The art
    /// compresses, reaches toward the target, flashes, then settles back into its
    /// natural idle. Only the child sprite moves, so collision and aiming stay fixed.
    public void PlayCleanupAttack(ElementForm form, Vector2 targetPosition)
    {
        if (!_sprites.TryGetValue(form, out var sprite)) return;

        CancelCleanupAttack();
        StopNaturalIdle(form);
        RestoreFormPose(form);
        _attackForm = form;

        Vector2 direction = (targetPosition - GlobalPosition).Normalized();
        if (direction == Vector2.Zero) direction = Vector2.Right;
        if (Mathf.Abs(direction.X) > 0.01f) sprite.FlipH = direction.X < 0f;

        Vector2 restScale = _restScales[form];
        Vector2 restPosition = _restPositions[form];
        Vector2 windupScale = form switch
        {
            ElementForm.Ice => new Vector2(restScale.X * 0.82f, restScale.Y * 1.10f),
            ElementForm.Electric => new Vector2(restScale.X * 0.90f, restScale.Y * 0.92f),
            _ => new Vector2(restScale.X * 0.86f, restScale.Y * 1.14f),
        };
        Vector2 releaseScale = form switch
        {
            ElementForm.Ice => new Vector2(restScale.X * 1.15f, restScale.Y * 0.86f),
            ElementForm.Electric => new Vector2(restScale.X * 1.24f, restScale.Y * 0.80f),
            _ => new Vector2(restScale.X * 1.20f, restScale.Y * 0.84f),
        };
        float releaseRotation = form == ElementForm.Electric
            ? Mathf.DegToRad(direction.X < 0f ? -7f : 7f)
            : Mathf.DegToRad(direction.Y * 3f);
        float windupTime = form == ElementForm.Electric ? 0.07f : 0.10f;
        float releaseTime = form == ElementForm.Ice ? 0.15f : 0.11f;
        Color attackColor = Colors.White.Lerp(Palette.ForForm(form), 0.32f);

        _attackTween = sprite.CreateTween();
        _attackTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _attackTween.TweenProperty(sprite, "scale", windupScale, windupTime);
        _attackTween.Parallel().TweenProperty(sprite, "position", restPosition - direction * 5f, windupTime);
        _attackTween.TweenProperty(sprite, "scale", releaseScale, releaseTime)
            .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(sprite, "position", restPosition + direction * 13f, releaseTime);
        _attackTween.Parallel().TweenProperty(sprite, "rotation", releaseRotation, releaseTime);
        _attackTween.Parallel().TweenProperty(sprite, "modulate", attackColor, releaseTime);
        _attackTween.TweenProperty(sprite, "scale", restScale, 0.18f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _attackTween.Parallel().TweenProperty(sprite, "position", restPosition, 0.18f);
        _attackTween.Parallel().TweenProperty(sprite, "rotation", 0f, 0.18f);
        _attackTween.Parallel().TweenProperty(sprite, "modulate", FormModulate(form), 0.18f);
        _attackTween.TweenCallback(Callable.From(() =>
        {
            RestoreFormPose(form);
            StartNaturalIdle(sprite, form);
            _attackTween = null;
            _attackForm = null;
        }));

        if (_cleanupBurst != null)
        {
            _cleanupBurst.Rotation = direction.Angle();
            _cleanupBurst.Scale = new Vector2(0.10f, 0.18f);
            _cleanupBurst.Modulate = new Color(attackColor.R, attackColor.G, attackColor.B, 0.90f);
            var burstTween = _cleanupBurst.CreateTween().SetParallel();
            _attackBurstTween = burstTween;
            burstTween.TweenProperty(_cleanupBurst, "scale", new Vector2(1.35f, 0.62f), 0.30f)
                .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
            burstTween.TweenProperty(_cleanupBurst, "modulate:a", 0f, 0.30f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
            burstTween.Chain().TweenCallback(Callable.From(() =>
            {
                if (_attackBurstTween == burstTween) _attackBurstTween = null;
            }));
        }
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
        PlayDamageImpact();
        Events.Instance?.EmitSignal(Events.SignalName.PlayerDamaged, amount);
        Events.Instance?.EmitSignal(Events.SignalName.PlayerHealthChanged, CurrentHealth, MaxHealthValue);
        AudioManager.Instance?.PlaySfx("player_hit");
        if (CurrentHealth == 0)
            SoftRespawn();
    }

    private void PlayDamageImpact()
    {
        CancelCleanupAttack();
        if (_sprites.TryGetValue(CurrentForm, out var active))
        {
            StopNaturalIdle(CurrentForm);
            RestoreFormPose(CurrentForm);
            Vector2 restScale = _restScales[CurrentForm];
            var spriteTween = active.CreateTween();
            spriteTween.TweenProperty(active, "scale", restScale * 1.22f, 0.07f)
                .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
            spriteTween.Parallel().TweenProperty(active, "modulate", new Color(1f, 0.28f, 0.20f, 1f), 0.05f);
            spriteTween.TweenProperty(active, "scale", restScale * 0.92f, 0.10f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            spriteTween.Parallel().TweenProperty(active, "modulate", FormModulate(CurrentForm), 0.10f);
            spriteTween.TweenProperty(active, "scale", restScale, 0.10f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            spriteTween.TweenCallback(Callable.From(() => StartNaturalIdle(active, CurrentForm)));
        }
        if (_damageBurst != null)
        {
            _damageBurst.Scale = Vector2.One * 0.18f;
            _damageBurst.Modulate = new Color(1f, 0.35f, 0.25f, 0.92f);
            var burstTween = _damageBurst.CreateTween().SetParallel();
            burstTween.TweenProperty(_damageBurst, "scale", Vector2.One * 1.55f, 0.22f)
                .SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
            burstTween.TweenProperty(_damageBurst, "modulate:a", 0f, 0.22f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        }
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

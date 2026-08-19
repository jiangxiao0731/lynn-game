using Godot;

namespace ShallowSeaDream;

/// res://scripts/UiFx.cs
/// Reusable button animation helper. Call UiFx.AnimateButton(btn) once per
/// button (idempotent). Wires hover / press / release via signals + Tween —
/// zero _Process overhead.
public static class UiFx
{
    // Metadata key used to detect duplicate wiring.
    private const string WiredKey = "_uifx_wired";

    /// <summary>
    /// Wire hover / press / release animations onto <paramref name="btn"/>.
    /// Safe to call multiple times on the same button (idempotent).
    /// Disabled buttons are skipped. If <paramref name="btn"/> is null,
    /// returns silently.
    /// </summary>
    public static void AnimateButton(Button? btn)
    {
        if (btn is null || btn.Disabled) return;
        if (btn.HasMeta(WiredKey)) return; // already wired

        btn.SetMeta(WiredKey, true);

        // Center pivot so scale-from-center works inside any container.
        // We set it immediately and also recenter on resize in case the
        // button has no size yet at wiring time.
        CenterPivot(btn);
        btn.Resized += () => CenterPivot(btn);

        // ── Hover ──────────────────────────────────────────────────────────
        btn.MouseEntered += () =>
        {
            if (btn.Disabled) return;
            var tw = btn.CreateTween();
            tw.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tw.TweenProperty(btn, "scale", new Vector2(1.02f, 1.02f), 0.12f);
            tw.Parallel().TweenProperty(btn, "modulate",
                new Color(1.06f, 1.06f, 1.02f, 1f), 0.12f);
        };
        btn.MouseExited += () =>
        {
            if (btn.Disabled) return;
            var tw = btn.CreateTween();
            tw.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tw.TweenProperty(btn, "scale", Vector2.One, 0.18f);
            tw.Parallel().TweenProperty(btn, "modulate", Colors.White, 0.18f);
        };

        // Focus (keyboard / gamepad) mirrors hover.
        btn.FocusEntered += () =>
        {
            if (btn.Disabled) return;
            var tw = btn.CreateTween();
            tw.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tw.TweenProperty(btn, "scale", new Vector2(1.02f, 1.02f), 0.12f);
        };
        btn.FocusExited += () =>
        {
            if (btn.Disabled) return;
            var tw = btn.CreateTween();
            tw.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tw.TweenProperty(btn, "scale", Vector2.One, 0.18f);
        };

        // ── Press ──────────────────────────────────────────────────────────
        btn.ButtonDown += () =>
        {
            if (btn.Disabled) return;
            var tw = btn.CreateTween();
            tw.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quint);
            tw.TweenProperty(btn, "scale", new Vector2(0.98f, 0.98f), 0.06f);
            tw.Parallel().TweenProperty(btn, "modulate",
                new Color(0.85f, 0.85f, 0.85f, 1f), 0.06f);
        };

        // ── Release / bounce ───────────────────────────────────────────────
        btn.ButtonUp += () =>
        {
            if (btn.Disabled) return;
            var tw = btn.CreateTween();
            tw.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quint);
            tw.TweenProperty(btn, "scale", Vector2.One, 0.14f);
            tw.Parallel().TweenProperty(btn, "modulate",
                new Color(1.08f, 1.08f, 1.02f, 1f), 0.07f);
            tw.TweenProperty(btn, "modulate", Colors.White, 0.07f);
        };
    }

    private static void CenterPivot(Control c)
    {
        c.PivotOffset = c.Size / 2f;
    }
}

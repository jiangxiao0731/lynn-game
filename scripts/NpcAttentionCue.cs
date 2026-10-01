using Godot;

namespace ShallowSeaDream;

/// Small visual hint for optional residents. It is intentionally quieter than the
/// objective arrow: a soft glow plus an occasional tiny pulse says "you can talk to
/// me" without making the NPC feel mandatory.
public static class NpcAttentionCue
{
    public static void Attach(Node2D node, Sprite2D sprite, Color tint, float displaySize)
    {
        node.AddToGroup("optional_npc");

        var glow = new Sprite2D
        {
            Name = "OptionalTalkGlow",
            Texture = PlaceholderArt.SoftGlow(new Color(tint.Lightened(0.25f), 0.50f)),
            ZIndex = sprite.ZIndex - 1,
            Modulate = new Color(1f, 1f, 1f, 0.16f),
        };
        glow.Scale = Vector2.One * Mathf.Max(1f, displaySize * 1.45f / 128f);
        node.AddChild(glow);

        float offset = (node.GetInstanceId() % 7) * 0.19f;
        var glowTween = glow.CreateTween().SetLoops();
        glowTween.TweenInterval(offset);
        glowTween.TweenProperty(glow, "modulate:a", 0.36f, 0.7f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        glowTween.TweenProperty(glow, "modulate:a", 0.12f, 1.0f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        glowTween.TweenInterval(1.4f);

        Vector2 restScale = sprite.Scale;
        var pulseTween = sprite.CreateTween().SetLoops();
        pulseTween.TweenInterval(0.5f + offset);
        pulseTween.TweenProperty(sprite, "scale", restScale * 1.07f, 0.18f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        pulseTween.TweenProperty(sprite, "scale", restScale, 0.32f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        pulseTween.TweenInterval(2.2f);
    }
}

using Godot;

namespace ShallowSeaDream;

/// res://scripts/BossAura.cs
/// Makes the guardian's end of the map read as a place rather than a coordinate.
///
/// Two layers, both driven by one distance value:
///   * a world-space stain on the seabed, so the arena is visible from far away and
///     the player can see where they are heading;
///   * a screen-space wash that only rises inside the approach radius, so crossing
///     into the arena is felt before the guardian is on screen.
///
/// The wash is deliberately capped well below opaque — it is a warning, not a filter,
/// and the painted backdrop has to stay readable underneath it.
public partial class BossAura : Node2D
{
    /// Nothing is tinted beyond this distance from the guardian.
    [Export] public float OuterRadius = 3600f;
    /// The wash is at full strength once the player is this close.
    [Export] public float InnerRadius = 1400f;
    /// Peak alpha of the screen wash.
    [Export] public float MaxWash = 0.34f;
    /// Radius of the seabed stain.
    [Export] public float FieldRadius = 2100f;

    private Node2D? _boss;
    private Node2D? _player;
    private TextureRect _wash = null!;
    private Line2D _rim = null!;
    private Node2D _field = null!;
    private Color _tint = new(0.62f, 0.16f, 0.22f);
    private float _pulse;
    private bool _announced;

    /// `tint` should be the chapter's pollution colour.
    public static BossAura Attach(Node parent, Node2D boss, Color tint)
    {
        var aura = new BossAura { Name = "BossAura", _tint = tint };
        // Set before AddChild: AddChild runs _Ready synchronously, and _Ready needs the
        // guardian to place the arena field. Assigning afterwards left it at the origin.
        aura._boss = boss;
        parent.AddChild(aura);
        return aura;
    }

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        // Seabed stain: one continuous radial falloff. Stacking translucent polygons
        // banded into visible rings, so the gradient is baked into a texture instead.
        _field = new Node2D { Name = "ArenaField", ZIndex = -60 };
        AddChild(_field);
        if (_boss != null) _field.Position = _boss.Position;

        var stain = new Sprite2D
        {
            Name = "ArenaStain",
            Texture = RadialFalloff(_tint, 0.34f, 512),
            // Flattened, so it lies on the seabed instead of hanging as a bubble.
            Scale = new Vector2(FieldRadius * 2f / 512f, FieldRadius * 2f * 0.62f / 512f),
        };
        _field.AddChild(stain);

        // A soft boundary glow rather than a stroked circle: still a readable edge,
        // but it fades into the stain instead of cutting a hard line across the art.
        _rim = new Line2D
        {
            Points = Ring(FieldRadius, 96), Closed = true, Width = 26f, ZIndex = 1,
            DefaultColor = new Color(_tint.Lerp(Colors.White, 0.5f), 0.34f),
            JointMode = Line2D.LineJointMode.Round,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
        };
        _field.AddChild(_rim);

        // Screen wash: a CanvasLayer under the HUD so panels stay legible.
        var layer = new CanvasLayer { Name = "BossAuraLayer", Layer = 1 };
        AddChild(layer);
        _wash = new TextureRect
        {
            Name = "ApproachVignette",
            Texture = RadialFalloff(_tint, 1f, 512, invert: true),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Modulate = new Color(1f, 1f, 1f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _wash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_wash);
    }

    public override void _Process(double delta)
    {
        if (_boss == null || _player == null || !IsInstanceValid(_boss) || !IsInstanceValid(_player)) return;

        float distance = _player.GlobalPosition.DistanceTo(_boss.GlobalPosition);
        // 0 outside the approach, 1 at the guardian.
        float nearness = 1f - Mathf.Clamp(
            (distance - InnerRadius) / Mathf.Max(1f, OuterRadius - InnerRadius), 0f, 1f);
        // Ease in, so the tint stays out of the way until the arena is genuinely close.
        nearness *= nearness;

        _pulse += (float)delta * 1.6f;
        float breath = 1f + 0.14f * Mathf.Sin(_pulse);

        _wash.Modulate = new Color(1f, 1f, 1f, MaxWash * nearness * breath);
        _field.Modulate = new Color(1f, 1f, 1f, 0.55f + 0.45f * nearness);
        _rim.Width = 26f + 10f * Mathf.Sin(_pulse * 1.3f);

        // Crossing the rim is announced once, so the boundary is not just ambient.
        bool inside = distance <= FieldRadius;
        if (inside && !_announced)
        {
            _announced = true;
            var flash = CreateTween();
            flash.TweenProperty(_wash, "modulate:a", Mathf.Min(1f, MaxWash * 2.2f), 0.18f);
            flash.TweenProperty(_wash, "modulate:a", MaxWash, 0.55f);
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                $"{ChapterRuntime.BossName} — you are inside its water");
        }
        else if (!inside && distance > FieldRadius * 1.25f) _announced = false;
    }

    /// A radial gradient sampled along a smooth curve. `invert` swaps it into a
    /// vignette (clear centre, tinted edges). Many stops on a cubic-interpolated
    /// gradient is what removes the banding a handful of stacked shapes produced.
    private static GradientTexture2D RadialFalloff(Color tint, float peakAlpha, int size,
        bool invert = false)
    {
        const int stops = 24;
        var offsets = new float[stops];
        var colors = new Color[stops];
        for (int i = 0; i < stops; i++)
        {
            float t = i / (stops - 1f);
            // smoothstep, then squared: dense near the centre, long soft tail outward.
            float falloff = 1f - t * t * (3f - 2f * t);
            falloff *= falloff;
            offsets[i] = t;
            colors[i] = new Color(tint, peakAlpha * (invert ? 1f - falloff : falloff));
        }

        var gradient = new Gradient
        {
            InterpolationMode = Gradient.InterpolationModeEnum.Cubic,
            Offsets = offsets,
            Colors = colors,
        };
        return new GradientTexture2D
        {
            Gradient = gradient, Width = size, Height = size,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
        };
    }

    private static Vector2[] Ring(float radius, int segments)
    {
        var points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.Tau * i / segments;
            // Slightly flattened, so it reads as ground rather than a floating bubble.
            points[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * 0.62f);
        }
        return points;
    }
}

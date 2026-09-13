using System;
using Godot;

namespace ShallowSeaDream;

/// res://scripts/ObjectiveGuide.cs
/// "Go here next", made impossible to miss.
///
/// The objective panel only ever said *what* to do. The chapters are ~17000px long,
/// so the target is off screen most of the time and the player had no way to know
/// which direction it was in. Two layers answer that:
///   * a big bouncing arrow over the target whenever it is on screen;
///   * a pinned arrow on the screen edge, pointing at it, with the distance, whenever
///     it is not.
/// Both use bright amber: the only warm, saturated colour in a teal world, so the
/// eye goes to it before it reads anything.
///
/// The level controller supplies the target through a callback, so the guide never
/// has to know how each chapter's objectives are structured.
public partial class ObjectiveGuide : Node2D
{
    /// Current target, or null when there is nothing to point at.
    public Func<Node2D?> Target = () => null;
    /// While true (a dialogue is open, for instance) the guide stays out of the way.
    public Func<bool> Suppressed = () => false;

    /// Within this distance the key prompt takes over; an arrow as well is clutter.
    private const float HandOffDistance = 250f;
    /// World pixels per displayed metre.
    private const float PixelsPerMetre = 100f;
    /// How far in from the screen edge the edge arrow sits.
    private const float EdgeInset = 76f;
    /// Screen band at the top taken by the objective panel; the arrow avoids it.
    private const float TopHudBand = 260f;
    /// Gap between the arrow tip and whatever it points at.
    private const float MarkerGap = 36f;
    /// Height of the arrow art above its tip.
    private const float MarkerHeight = 95f;

    private static readonly Color Amber = new(1.00f, 0.78f, 0.22f);
    private static readonly Color Ink = new(0.07f, 0.05f, 0.03f, 0.92f);

    private Node2D _worldMarker = null!;
    private Control _edge = null!;
    private Node2D _edgeArrow = null!;
    private Label _edgeDistance = null!;
    private float _time;

    public static ObjectiveGuide Attach(Node parent, Func<Node2D?> target, Func<bool>? suppressed = null)
    {
        var guide = new ObjectiveGuide { Name = "ObjectiveGuide", Target = target };
        if (suppressed != null) guide.Suppressed = suppressed;
        parent.AddChild(guide);
        return guide;
    }

    public override void _Ready()
    {
        // World marker: a fat downward chevron with a dark outline, like a sticker.
        _worldMarker = new Node2D { Name = "TargetArrow", ZIndex = 40, Visible = false };
        AddChild(_worldMarker);
        var chevron = new[]
        {
            new Vector2(-34, -58), new Vector2(34, -58), new Vector2(34, -22),
            new Vector2(58, -22), new Vector2(0, 30), new Vector2(-58, -22), new Vector2(-34, -22),
        };
        _worldMarker.AddChild(new Polygon2D { Polygon = Outset(chevron, 7f), Color = Ink });
        _worldMarker.AddChild(new Polygon2D { Polygon = chevron, Color = Amber });
        _worldMarker.AddChild(new Polygon2D
        {
            // Highlight on the upper-left, so it reads as a glossy object, not a flat icon.
            Polygon = new[] { new Vector2(-26, -50), new Vector2(-4, -50), new Vector2(-4, -30), new Vector2(-26, -30) },
            Color = new Color(1f, 1f, 1f, 0.45f),
        });

        // Edge arrow lives in screen space, above the world and below the HUD panels.
        var layer = new CanvasLayer { Name = "GuideLayer", Layer = 4 };
        AddChild(layer);
        _edge = new Control { Name = "EdgePointer", MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        layer.AddChild(_edge);

        _edgeArrow = new Node2D { Name = "Arrow" };
        _edge.AddChild(_edgeArrow);
        var disc = Circle(44f, 40);
        _edgeArrow.AddChild(new Polygon2D { Polygon = Circle(50f, 40), Color = Ink });
        _edgeArrow.AddChild(new Polygon2D { Polygon = disc, Color = Amber });
        var head = new[] { new Vector2(30, 0), new Vector2(-10, -24), new Vector2(-2, 0), new Vector2(-10, 24) };
        _edgeArrow.AddChild(new Polygon2D { Polygon = head, Color = Ink });

        _edgeDistance = UiTheme.Role(UiTheme.TypeRole.Name, "");
        _edgeDistance.HorizontalAlignment = HorizontalAlignment.Center;
        _edgeDistance.AddThemeColorOverride("font_color", Amber);
        _edgeDistance.AddThemeColorOverride("font_outline_color", Ink);
        _edgeDistance.AddThemeConstantOverride("outline_size", 8);
        _edgeDistance.Size = new Vector2(160, 40);
        _edge.AddChild(_edgeDistance);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        var target = Target();
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        bool active = target != null && IsInstanceValid(target) && target.IsVisibleInTree()
                      && player != null && !Suppressed();
        if (!active)
        {
            _worldMarker.Visible = false;
            _edge.Visible = false;
            return;
        }

        float distance = player!.GlobalPosition.DistanceTo(target!.GlobalPosition);
        var viewport = GetViewport();
        Vector2 view = viewport.GetVisibleRect().Size;
        Vector2 onScreen = viewport.GetCanvasTransform() * target.GlobalPosition;
        bool visible = onScreen.X > EdgeInset && onScreen.X < view.X - EdgeInset
                       && onScreen.Y > EdgeInset && onScreen.Y < view.Y - EdgeInset;

        // On screen: bounce over the target, until the key prompt takes over.
        _worldMarker.Visible = visible && distance > HandOffDistance;
        if (_worldMarker.Visible)
        {
            float bounce = Mathf.Abs(Mathf.Sin(_time * 4.2f)) * 22f;
            var above = target.GlobalPosition + new Vector2(0, -Extent(target, up: true) - MarkerGap - bounce);
            // Targets high on the map would put the arrow under the objective panel;
            // there it flips underneath the target and points up instead.
            float arrowTopOnScreen = (viewport.GetCanvasTransform() * above).Y - MarkerHeight;
            bool flip = arrowTopOnScreen < TopHudBand;
            _worldMarker.GlobalPosition = flip
                ? target.GlobalPosition + new Vector2(0, Extent(target, up: false) + MarkerGap + bounce)
                : above;
            _worldMarker.Rotation = flip ? Mathf.Pi : 0f;
            float squash = 1f + 0.08f * Mathf.Sin(_time * 8.4f);
            _worldMarker.Scale = new Vector2(squash, 2f - squash);
        }

        // Off screen: pin to the edge along the line from the centre to the target.
        _edge.Visible = !visible;
        if (_edge.Visible)
        {
            Vector2 centre = view * 0.5f;
            Vector2 dir = (onScreen - centre).Normalized();
            float halfW = centre.X - EdgeInset, halfH = centre.Y - EdgeInset;
            float reach = Mathf.Min(halfW / Mathf.Max(Mathf.Abs(dir.X), 0.0001f),
                                    halfH / Mathf.Max(Mathf.Abs(dir.Y), 0.0001f));
            Vector2 pin = centre + dir * reach;
            float nudge = Mathf.Sin(_time * 5f) * 8f;   // a small push toward the target
            _edgeArrow.Position = pin + dir * nudge;
            _edgeArrow.Rotation = dir.Angle();
            _edgeArrow.Scale = Vector2.One * (1f + 0.06f * Mathf.Sin(_time * 5f));

            // Distance sits on the inner side of the arrow and never rotates.
            _edgeDistance.Text = $"{Mathf.Max(1, Mathf.RoundToInt(distance / PixelsPerMetre))} m";
            _edgeDistance.Position = pin - dir * 112f - _edgeDistance.Size * 0.5f;
        }
    }

    /// How far the target's art and nameplates reach above (up) or below its origin,
    /// so the arrow clears both. Chapter one draws names above NPCs and chapters two
    /// and three below them; measuring only the sprite put the arrow on the name.
    private static float Extent(Node2D target, bool up)
    {
        float reach = 0f;
        // `offset` is the node's Y in the target's space: 0 for the target itself,
        // its Position for a child.
        void Consider(Node node, float offset)
        {
            float lo, hi;
            switch (node)
            {
                case Sprite2D { Texture: not null } s:
                    float sh = s.Texture.GetHeight() * Mathf.Abs(s.Scale.Y) * 0.5f;
                    lo = offset - sh; hi = offset + sh;
                    break;
                case AnimatedSprite2D a when a.SpriteFrames?.GetFrameTexture("Idle", 0) is { } f:
                    float ah = f.GetHeight() * Mathf.Abs(a.Scale.Y) * 0.5f;
                    lo = offset - ah; hi = offset + ah;
                    break;
                case Control { Visible: true } c:
                    lo = offset; hi = offset + c.Size.Y;
                    break;
                default:
                    return;
            }
            reach = up ? Mathf.Max(reach, -lo) : Mathf.Max(reach, hi);
        }
        Consider(target, 0f);
        foreach (var child in target.GetChildren())
        {
            float y = child switch { Node2D n => n.Position.Y, Control c => c.Position.Y, _ => 0f };
            Consider(child, y);
        }
        return reach > 0f ? reach : 60f;
    }

    private static Vector2[] Circle(float radius, int segments)
    {
        var points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
            points[i] = Vector2.FromAngle(Mathf.Tau * i / segments) * radius;
        return points;
    }

    /// Crude outline: push every vertex away from the centroid.
    private static Vector2[] Outset(Vector2[] shape, float amount)
    {
        Vector2 centroid = Vector2.Zero;
        foreach (var p in shape) centroid += p;
        centroid /= shape.Length;
        var result = new Vector2[shape.Length];
        for (int i = 0; i < shape.Length; i++)
            result[i] = shape[i] + (shape[i] - centroid).Normalized() * amount;
        return result;
    }
}

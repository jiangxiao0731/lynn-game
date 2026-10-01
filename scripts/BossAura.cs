using Godot;

namespace ShallowSeaDream;

/// Colour atmosphere for a guardian arena. It deliberately has no circular outline:
/// pollution arrives as a broad directional wash across the painted water. The wash
/// is world-space and below entities, so the guardian itself always stays crisp on top.
public partial class BossAura : Node2D
{
    [Export] public float OuterRadius = 3600f;
    [Export] public float InnerRadius = 1400f;
    [Export] public float MaxWash = 0.38f;
    [Export] public float FieldWidth = 5200f;

    private Node2D? _boss;
    private Node2D? _player;
    private TextureRect _wash = null!;
    private Color _tint = new(0.62f, 0.16f, 0.22f);
    private float _pulse;
    private bool _announced;

    public static BossAura Attach(Node parent, Node2D boss, Color tint)
    {
        var aura = new BossAura { Name = "BossAura", _boss = boss, _tint = tint };
        parent.AddChild(aura);
        return aura;
    }

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (_boss == null) return;

        // Cover the whole chapter with a very soft directional tint. Earlier builds
        // used a local polygon/current near the guardian; its edge could read as a
        // visible line in the boss area. A full-map wash has no local boundary.
        float mapWidth = ChapterMap.TotalWidth(ChapterRuntime.CurrentChapter);
        _wash = new TextureRect
        {
            Name = "ArenaColourWash",
            Position = Vector2.Zero,
            Size = new Vector2(mapWidth, ChapterMap.Height),
            Texture = PlaceholderArt.HorizontalGradient(
                new Color(_tint, MaxWash * 0.15f), new Color(_tint.Lightened(0.08f), MaxWash)),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = -4,
            Modulate = new Color(1f, 1f, 1f, 0.42f),
        };
        AddChild(_wash);
    }

    public override void _Process(double delta)
    {
        if (_boss == null || _player == null || !IsInstanceValid(_boss) || !IsInstanceValid(_player)) return;

        float distance = _player.GlobalPosition.DistanceTo(_boss.GlobalPosition);
        float nearness = 1f - Mathf.Clamp(
            (distance - InnerRadius) / Mathf.Max(1f, OuterRadius - InnerRadius), 0f, 1f);
        nearness *= nearness;
        _pulse += (float)delta * 1.35f;
        float breath = 0.88f + 0.12f * Mathf.Sin(_pulse);

        if (IsInstanceValid(_wash))
            _wash.Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(0.42f, 1f, nearness) * breath);

        bool inside = distance <= InnerRadius + 520f;
        if (inside && !_announced)
        {
            _announced = true;
            Events.Instance?.EmitSignal(Events.SignalName.StatusHint,
                $"{ChapterRuntime.BossName} — polluted water is closing in");
        }
        else if (!inside && distance > OuterRadius) _announced = false;
    }
}

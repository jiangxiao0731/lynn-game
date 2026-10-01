using System;
using Godot;

namespace ShallowSeaDream;

/// A quieter companion to ObjectiveGuide. ObjectiveGuide points at the required
/// story step; this only helps optional NPCs and residents get noticed across the
/// long multi-panel maps.
public partial class OptionalInteractionGuide : Node2D
{
    public Func<bool> Suppressed = () => false;

    private const float ActiveDistance = 1800f;
    private const float HandOffDistance = 260f;
    private const float EdgeInset = 98f;
    private const float TopHudBand = 290f;

    private Control _tag = null!;
    private Label _label = null!;
    private float _time;

    public static OptionalInteractionGuide Attach(Node parent, Func<bool>? suppressed = null)
    {
        var guide = new OptionalInteractionGuide { Name = "OptionalInteractionGuide" };
        if (suppressed != null) guide.Suppressed = suppressed;
        parent.AddChild(guide);
        return guide;
    }

    public override void _Ready()
    {
        var layer = new CanvasLayer { Name = "OptionalGuideLayer", Layer = 3 };
        AddChild(layer);
        _tag = new PanelContainer { Name = "TalkHint", Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _tag.AddThemeStyleboxOverride("panel", UiTheme.GlassPanel(8, 0.68f, UiTheme.GapPair));
        layer.AddChild(_tag);
        _label = UiTheme.WorldRole(UiTheme.TypeRole.Eyebrow, "Talk");
        _label.AddThemeColorOverride("font_color", UiTheme.Accent);
        _label.Size = new Vector2(120, 34);
        _tag.AddChild(_label);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player == null || Suppressed())
        {
            _tag.Visible = false;
            return;
        }

        Node2D? target = null;
        float best = ActiveDistance;
        foreach (var node in GetTree().GetNodesInGroup("optional_npc"))
        {
            if (node is not Node2D npc || !IsInstanceValid(npc) || !npc.IsVisibleInTree()) continue;
            float dist = npc.GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist < best)
            {
                best = dist;
                target = npc;
            }
        }

        if (target == null || best <= HandOffDistance)
        {
            _tag.Visible = false;
            return;
        }

        var viewport = GetViewport();
        Vector2 view = viewport.GetVisibleRect().Size;
        Vector2 screen = viewport.GetCanvasTransform() * target.GlobalPosition;
        bool visible = screen.X > EdgeInset && screen.X < view.X - EdgeInset
                       && screen.Y > TopHudBand && screen.Y < view.Y - EdgeInset;

        Vector2 pos;
        if (visible)
        {
            pos = screen + new Vector2(-60f, -120f + Mathf.Sin(_time * 4f) * 8f);
        }
        else
        {
            Vector2 centre = view * 0.5f;
            Vector2 dir = (screen - centre).Normalized();
            float halfW = centre.X - EdgeInset, halfH = centre.Y - EdgeInset;
            float reach = Mathf.Min(halfW / Mathf.Max(Mathf.Abs(dir.X), 0.0001f),
                                    halfH / Mathf.Max(Mathf.Abs(dir.Y), 0.0001f));
            pos = centre + dir * reach - new Vector2(60f, 18f);
            if (pos.Y < TopHudBand) pos.Y = TopHudBand;
        }

        _tag.Position = pos;
        _tag.Visible = true;
    }
}

using Godot;

namespace ShallowSeaDream;

/// Shared physical spacing for anything the player talks to or examines. The art is
/// intentionally much larger than the old 36px player hitbox, so interaction targets
/// need their own silhouette-sized body instead of allowing the two paintings to stack.
public static class InteractionSpacing
{
    public const uint WorldCollisionLayer = 32;
    public const float NpcConversationDistance = 190f;
    public const float GuardianConversationDistance = 360f;

    public static void AddSolid(Node2D host, float displaySize, float minimumRadius = 54f)
    {
        var body = new StaticBody2D
        {
            Name = "InteractionBody",
            CollisionLayer = WorldCollisionLayer,
            CollisionMask = 0,
        };
        body.AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D
            {
                Radius = Mathf.Clamp(displaySize * 0.52f, minimumRadius, 132f),
            },
        });
        host.AddChild(body);
    }

    /// Put Shimmer at a readable two-shot distance before freezing movement for a
    /// conversation. Normally the solid silhouette already enforces most of this;
    /// this small correction also repairs old saves or scripted routes that approach
    /// from inside an art silhouette.
    public static void FrameConversation(Player player, Node2D partner, float preferredDistance)
    {
        Vector2 away = player.GlobalPosition - partner.GlobalPosition;
        if (away.LengthSquared() < 1f)
            away = Vector2.Left;
        float distance = away.Length();
        if (distance < preferredDistance)
        {
            Vector2 desired = partner.GlobalPosition + away.Normalized() * preferredDistance;
            desired.X = Mathf.Clamp(desired.X, 72f, ChapterMap.TotalWidth(ChapterRuntime.CurrentChapter) - 72f);
            desired.Y = Mathf.Clamp(desired.Y, 88f, ChapterMap.Height - 88f);
            player.GlobalPosition = desired;
            player.Velocity = Vector2.Zero;
        }
        player.FaceTowards(partner.GlobalPosition);
    }
}

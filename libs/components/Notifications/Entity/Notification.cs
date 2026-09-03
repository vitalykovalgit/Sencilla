namespace Sencilla.Component.Notifications;

/// <summary>
/// An in-app notification. Written by trusted app code (workers, endpoints — never clients) via
/// the application; who receives it is <see cref="NotificationTarget"/>'s job and
/// whether a given user has read it is <see cref="NotificationRead"/>'s.
///
/// Deliberately has no <c>UserId</c> and no <c>[CrudApi]</c>: one row can serve one user, a role
/// or everybody, and clients read the flattened <see cref="NotificationInbox"/> instead.
/// </summary>
public class Notification : IEntity<Guid>, IEntityCreateableTrack
{
    public Guid Id { get; set; }

    /// <summary>Wire key for the notification class — see <see cref="NotificationType.KeyOf(Type)"/>.</summary>
    public required string Type { get; set; }

    /// <summary>
    /// Serialized notification class — ids and error CODES only, never display strings.
    /// <see cref="JsonObjectStringAttribute"/> is a System.Text.Json converter (object on the wire,
    /// string in C#); it has no effect on the column, and this entity has no API surface of its own —
    /// <see cref="NotificationInbox"/> carries the same attribute for the wire.
    /// </summary>
    [JsonObjectString]
    public string? Data { get; set; }

    public DateTime CreatedDate { get; set; }
}

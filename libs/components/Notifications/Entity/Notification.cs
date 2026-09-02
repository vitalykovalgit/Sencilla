namespace Sencilla.Component.Notifications;

/// <summary>
/// An in-app notification. Written by trusted app code (workers, endpoints — never clients) via
/// <see cref="NotificationWriter"/>; who receives it is <see cref="NotificationTarget"/>'s job and
/// whether a given user has read it is <see cref="NotificationRead"/>'s.
///
/// Deliberately has no <c>UserId</c> and no <c>[CrudApi]</c>: one row can serve one user, a role
/// or everybody, and clients read the flattened <see cref="NotificationInbox"/> instead.
/// </summary>
public class Notification : IEntity<Guid>, IEntityCreateableTrack
{
    /// <summary>
    /// Primary key. Application-generated sequential GUID (EF SequentialGuidValueGenerator).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Wire key for the notification class — see <see cref="NotificationType.KeyOf(Type)"/>.
    /// Persisted forever, so a class that may be renamed should declare an explicit
    /// <see cref="NotificationTypeAttribute"/> alias.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Serialized notification class — ids and error CODES only (the client owns all wording).
    /// </summary>
    [JsonObjectString]
    public string? Data { get; set; }

    public DateTime CreatedDate { get; set; }
}

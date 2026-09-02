namespace Sencilla.Component.Notifications;

/// <summary>
/// The client-facing read surface: every <see cref="Notification"/> flattened to the users it
/// reaches, with that user's read stamp resolved. Mapped to the <c>NotificationInboxView</c> the
/// component ships — EF maps an entity onto a view exactly as onto a table.
///
/// Read-only by construction (no create/update markers) and, more importantly, by grant: scope it
/// with a matrix row on resource <c>notificationinboxview</c>, Read with <c>userId={user}.Id</c>.
/// The view itself contains no authorization — it resolves audience, not permission — so that one
/// grant is the whole authorization story and must not be widened.
///
/// One notification appears once per recipient, so <see cref="Id"/> repeats across users. That is
/// invisible to the API (every read is scoped to a single user and materializes with
/// <c>AsNoTracking</c>), but it does mean this entity must never be loaded through a tracked query.
/// </summary>
[Table("NotificationInboxView")]
[CrudApi("api/v1/notifications")]
public class NotificationInbox : IEntity<Guid>
{
    /// <summary>The notification's id — unique per user, not per row.</summary>
    public Guid Id { get; set; }

    /// <summary>Recipient. The matrix constraint scopes every read to this column.</summary>
    public Guid UserId { get; set; }

    /// <summary>Wire key for the notification class — see <see cref="NotificationType.KeyOf(Type)"/>.</summary>
    public string Type { get; set; } = "";

    /// <summary>Serialized notification class — ids and error CODES only.</summary>
    [JsonObjectString]
    public string? Data { get; set; }

    public DateTime CreatedDate { get; set; }

    /// <summary>Null = unread for this user, whether by watermark or by an explicit read row.</summary>
    public DateTime? ReadDate { get; set; }

    /// <summary>
    /// Derived from <see cref="ReadDate"/> so the badge can be a server-side count
    /// (<c>?isUnread=true</c>). A bool renders through the typed comparison path; an "is null"
    /// filter on a DateTime? has no reliable representation in the query-string binder.
    /// </summary>
    public bool IsUnread { get; set; }
}

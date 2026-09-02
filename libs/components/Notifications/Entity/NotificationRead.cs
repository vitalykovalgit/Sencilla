namespace Sencilla.Component.Notifications;

/// <summary>
/// Per-user read state.
///
/// A row with a null <see cref="NotificationId"/> is a WATERMARK: the user has read everything
/// created at or before <see cref="ReadDate"/>. That is what marking the notification panel read
/// writes — one row per user rather than one per notification per user, and it covers rows past
/// the loaded page too. Per-notification rows are supported by the schema for a future per-item
/// "mark read" action; nothing writes them today.
///
/// Client-writable, uniquely in this component. Scope it with a matrix grant on resource
/// <c>notificationread</c>: Create and Read with <c>userId={user}.Id</c>, and NO Update or Delete
/// — a CrudApi update is a full-row replace, and the read watermark is the one thing a user could
/// usefully forge.
/// </summary>
[CrudApi("api/v1/notifications/read")]
public class NotificationRead : IEntity<Guid>, IEntityCreateableTrack
{
    /// <summary>
    /// Primary key. Application-generated sequential GUID (EF SequentialGuidValueGenerator).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Null = watermark; otherwise the one notification this row marks read.</summary>
    public Guid? NotificationId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Stamped server-side — <see cref="IEntityCreateableTrack"/> makes the create repository
    /// overwrite whatever arrived. Never trust a client value here: notification CreatedDate comes
    /// from the worker's clock, so a browser clock running fast would set a watermark into the
    /// future and silently mark notifications read before they are written.
    /// </summary>
    [Column("ReadDate")]
    public DateTime CreatedDate { get; set; }
}

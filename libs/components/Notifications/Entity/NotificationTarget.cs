namespace Sencilla.Component.Notifications;

/// <summary>
/// Audience scopes resolved by <see cref="NotificationInbox"/>. Values at or above
/// <see cref="AppScope"/> are the consuming app's own — this component neither defines nor
/// resolves them.
/// </summary>
public enum NotificationScope : byte
{
    /// <summary>One user: <see cref="NotificationTarget.TargetGuid"/> is their id.</summary>
    User = 1,

    /// <summary>Everyone holding a role: <see cref="NotificationTarget.TargetInt"/> is a sec.Role id.</summary>
    Role = 2,

    /// <summary>Everybody. Neither target slot is used, and it stays ONE row however many users exist.</summary>
    System = 3,

    /// <summary>
    /// First value a consuming app may define. An app either expands its own scopes into
    /// <see cref="User"/> rows when it writes, or supplies its own recipient view.
    /// </summary>
    AppScope = 100,
}

/// <summary>
/// Who receives a <see cref="Notification"/>. Multiple rows UNION — each row widens the audience,
/// matching how security matrix grants accumulate — while the two target slots within one row AND,
/// so an intersection such as "owners of project X" is a single row rather than something union
/// semantics cannot express.
///
/// Not client-facing: no <c>[CrudApi]</c>, written only through <see cref="NotificationWriter"/>.
/// </summary>
public class NotificationTarget : IEntity<Guid>
{
    /// <summary>
    /// Primary key. Application-generated sequential GUID (EF SequentialGuidValueGenerator).
    /// </summary>
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    /// <summary>See <see cref="NotificationScope"/>.</summary>
    public byte Scope { get; set; }

    /// <summary>
    /// GUID-keyed target: the user id for <see cref="NotificationScope.User"/>, or an app scope's
    /// own entity id. No FK — the target is polymorphic and lives in components this one does not
    /// depend on.
    /// </summary>
    public Guid? TargetGuid { get; set; }

    /// <summary>INT-keyed target: a sec.Role id for <see cref="NotificationScope.Role"/>.</summary>
    public int? TargetInt { get; set; }
}

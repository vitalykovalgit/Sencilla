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
/// Not client-facing: no <c>[CrudApi]</c>. Written by the application, in whatever transaction the
/// domain change it announces runs in — see <see cref="NotificationRepositoryEx.ToUser"/>, which is
/// an extension over the caller's repositories for exactly that reason.
/// </summary>
public class NotificationTarget : IEntity<Guid>, IEntityCreateable
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    /// <summary>Values are <see cref="NotificationScope"/>.</summary>
    public byte Scope { get; set; }

    public Guid? TargetGuid { get; set; }

    public int? TargetInt { get; set; }
}

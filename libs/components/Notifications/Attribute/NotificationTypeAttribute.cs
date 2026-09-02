namespace Sencilla.Component.Notifications;

/// <summary>
/// The wire key a notification class is stored and rendered under, overriding the default (the
/// class name without its namespace).
///
/// Declare one whenever the class might be renamed or moved: the key is persisted forever, and
/// the client switches on the same string, so a rename without an alias strands every row already
/// written and silently changes what new ones say.
/// </summary>
/// <example>
/// <code>
/// [NotificationType("project.cloned")]
/// public class ProjectClonedNotification
/// {
///     public Guid ProjectId { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public class NotificationTypeAttribute(string name) : Attribute
{
    /// <summary>The persisted wire key.</summary>
    public string Name { get; } = name;
}

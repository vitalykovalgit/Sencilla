namespace Sencilla.Component.Notifications;

/// <summary>
/// How a notification class becomes a <see cref="Notification"/> row: the wire key it is stored
/// and rendered under, and the JSON encoding of its payload.
///
/// Deliberately NOT <c>Sencilla.Messaging.PayloadTypeRegistry</c>, whose fallback is the type's
/// FULL name. The two solve different problems. A message key must round-trip back to a CLR type
/// in a consuming process, so it has to stay globally unambiguous and is resolved by scanning
/// every loaded assembly, taking the first match. A notification key is write-only — nothing ever
/// resolves it back, because the client renders from the string — which is exactly what makes a
/// short, readable, namespace-free default safe here and unsafe there.
/// </summary>
/// <example>
/// <code>
/// var notification = new Notification
/// {
///     Type = NotificationType.KeyOf&lt;ProjectClonedNotification&gt;(),
///     Data = NotificationType.Serialize(new ProjectClonedNotification { ProjectId = id }),
///     CreatedDate = DateTime.UtcNow,
/// };
/// </code>
/// </example>
public static class NotificationType
{
    /// <summary>
    /// Shared so every writer encodes <see cref="Notification.Data"/> identically. Web defaults
    /// (camelCase) because the client reads these property names directly.
    /// </summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The wire key for a notification class: its <see cref="NotificationTypeAttribute"/> alias
    /// when it declares one, otherwise the class name without its namespace.
    /// </summary>
    public static string KeyOf(Type type)
        => type.GetCustomAttribute<NotificationTypeAttribute>(false)?.Name ?? type.Name;

    /// <inheritdoc cref="KeyOf(Type)"/>
    public static string KeyOf<T>() => KeyOf(typeof(T));

    /// <summary>Encodes a notification class into <see cref="Notification.Data"/>.</summary>
    public static string Serialize<T>(T payload) => JsonSerializer.Serialize(payload, Json);

    // ponytail: no cache and no assembly scan, because nothing resolves a key back to a type;
    // and no writer/service wrapping this, because creating the rows is two repository calls the
    // caller already has. Add a scan the day an admin composer needs to LIST the available
    // notification classes — that is the one thing a real registry would buy.
}

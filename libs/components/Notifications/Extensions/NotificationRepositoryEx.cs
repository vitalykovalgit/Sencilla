namespace Sencilla.Component.Notifications;

/// <summary>
/// Writing one notification is two inserts — the row, and at least one target that says who it is
/// for — so every writer would otherwise restate that pairing and re-derive the type key.
///
/// Deliberately an extension over the caller's repositories rather than an injectable service: it
/// runs on the DbContext and transaction the caller already has open, which is what lets a
/// background handler commit a notification together with the domain change it announces instead
/// of announcing something that then rolls back.
/// </summary>
public static class NotificationRepositoryEx
{
    /// <summary>
    /// One notification addressed to one user.
    /// </summary>
    public static async Task ToUser<T>(
        this ICreateRepository<Notification, Guid> notifications,
        ICreateRepository<NotificationTarget, Guid> targets,
        T payload,
        Guid userId,
        CancellationToken token = default)
    {
        // CreatedDate is stamped by the create repository — Notification is IEntityCreateableTrack.
        var notification = await notifications.Create(new Notification
        {
            Type = NotificationType.KeyOf<T>(),
            Data = NotificationType.Serialize(payload),
        }, token) ?? throw new InvalidOperationException("notification.create_failed");

        // Never skipped when the insert above fails: a target with no notification addresses
        // nobody, and no constraint would catch it.
        await targets.Create(new NotificationTarget
        {
            NotificationId = notification.Id,
            Scope = (byte)NotificationScope.User,
            TargetGuid = userId,
        }, token);
    }
}

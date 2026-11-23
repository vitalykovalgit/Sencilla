namespace Sencilla.Component.Users;

/// <summary>
/// The contact confirmation flags (<see cref="User.EmailConf"/>, <see cref="User.PhoneConf"/>) are facts the
/// framework establishes, never client input — as attribution is in <see cref="TrackStampHandler{TEntity}"/>.
/// A write as the current user keeps a flag the stored row has only while its address is unchanged and can never
/// raise one: a confirmed number stays confirmed while it is the same number, an edited one becomes a typed
/// (unconfirmed) one, and a row a client creates starts unconfirmed. A write under <see cref="Access.Root"/> is the
/// framework's own — the authentication store confirming an address or creating an account from a verified
/// identity — and is left exactly as written. The stored rows are the update's own pre-image
/// (<see cref="EntityUpdatingEvent{TEntity}.DbEntities"/>: one non-tracking query, ids IN …), as the audit handler
/// reads them. Auto-registered by the event registrator.
/// </summary>
public class ContactConfirmationGuard
    : IEventHandlerBase<EntityCreatingEvent<User>>
    , IEventHandlerBase<EntityUpdatingEvent<User>>
    , IEventHandlerBase<EntityUpdatingEvent<UserPhone>>
{
    public Task HandleAsync(EntityCreatingEvent<User> @event, CancellationToken token)
    {
        if (IsFrameworkWrite())
            return Task.CompletedTask;

        foreach (var user in @event?.Entities ?? Enumerable.Empty<User>().AsQueryable())
        {
            user.EmailConf = false;
            user.PhoneConf = false;
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(EntityUpdatingEvent<User> @event, CancellationToken token)
    {
        var incoming = @event?.Entities?.ToList() ?? [];
        if (incoming.Count == 0 || IsFrameworkWrite())
            return Task.CompletedTask;

        var stored = (@event!.DbEntities?.ToList() ?? []).ToDictionary(u => u.Id);
        foreach (var user in incoming)
        {
            var row = stored.GetValueOrDefault(user.Id);
            user.EmailConf = row is { EmailConf: true } && IsSameAddress(row.Email, user.Email);
            user.PhoneConf = row is { PhoneConf: true } && row.Phone == user.Phone;
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(EntityUpdatingEvent<UserPhone> @event, CancellationToken token)
    {
        var incoming = @event?.Entities?.ToList() ?? [];
        if (incoming.Count == 0 || IsFrameworkWrite())
            return Task.CompletedTask;

        var stored = (@event!.DbEntities?.ToList() ?? []).ToDictionary(p => p.Id);
        foreach (var phone in incoming)
        {
            var row = stored.GetValueOrDefault(phone.Id);
            phone.PhoneConf = row is { PhoneConf: true } && row.Phone == phone.Phone;
        }

        return Task.CompletedTask;
    }

    /// <summary>The same test entity security uses for «acting as the system».</summary>
    private static bool IsFrameworkWrite() => Access.Current?.AllowAll ?? false;

    /// <summary>Addresses compare as the authentication family stores them: trimmed, case-insensitive.</summary>
    private static bool IsSameAddress(string? stored, string? incoming)
        => string.Equals(stored?.Trim(), incoming?.Trim(), StringComparison.OrdinalIgnoreCase);
}

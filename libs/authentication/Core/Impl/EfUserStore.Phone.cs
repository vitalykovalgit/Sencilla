namespace Sencilla.Authentication;

/// <summary>
/// The confirmed phone of an account: what a provider-verified number (Telegram's) makes of it, at account creation and
/// when the provider is attached to an existing account, and the hint that a confirmed phone is typed elsewhere.
/// </summary>
public sealed partial class EfUserStore
{
    public async Task<bool> IsConfirmedPhoneOnAnotherAccount(Guid userId, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var phone = await _userRead.Query.AsNoTracking()
            .Where(u => u.Id == userId && u.PhoneConf)
            .Select(u => u.Phone)
            .FirstOrDefaultAsync(token);
        return phone != 0 && await _userRead.Query.AsNoTracking()
            .AnyAsync(u => u.Phone == phone && u.Id != userId && u.DeletedDate == null, token);
    }

    /// <summary>
    /// The phone an account holding <paramref name="phone"/> has once <paramref name="identity"/>'s provider-verified
    /// number reaches it: that number, when the account has none or the same one — confirmed unless another account has
    /// it confirmed — else the account's own, untouched: never over a number the user entered.
    /// </summary>
    private async Task<(long Phone, bool IsConfirmed)> PhoneWithProvidersNumber(
        Guid accountId, long phone, bool isConfirmed, ExternalIdentity? identity, CancellationToken token)
    {
        if (isConfirmed || identity is not { PhoneVerified: true, Phone: { } verified and not 0 } || (phone != 0 && phone != verified))
            return (phone, isConfirmed);
        return (verified, !await IsPhoneConfirmedElsewhere(verified, accountId, token));
    }

    /// <summary>An existing account gets the provider's verified number by <see cref="PhoneWithProvidersNumber"/>.</summary>
    private async Task WriteProvidersPhone(Guid userId, ExternalIdentity identity, CancellationToken token)
    {
        if (await _userRead.GetById(userId, token) is not { } user)
            return;

        var (phone, isConfirmed) = await PhoneWithProvidersNumber(userId, user.Phone, user.PhoneConf, identity, token);
        if (phone == user.Phone && isConfirmed == user.PhoneConf)
            return;

        user.Phone = phone;
        user.PhoneConf = isConfirmed;
        await _userUpdate.Update(user, token);
    }

    // ponytail: a recycled number — the first account keeps the confirmation; revisit if it ever matters.
    /// <summary>
    /// A confirmed phone belongs to one account (the filtered unique index <c>UX_User_Phone</c>), so another account
    /// the provider gives the same verified number keeps it as a typed, unconfirmed one.
    /// </summary>
    private Task<bool> IsPhoneConfirmedElsewhere(long phone, Guid accountId, CancellationToken token)
        => _userRead.Query.AsNoTracking().AnyAsync(u => u.Phone == phone && u.PhoneConf && u.Id != accountId, token);
}

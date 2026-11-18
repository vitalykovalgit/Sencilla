namespace Sencilla.Authentication;

/// <summary>
/// Default <see cref="IAccountLinkService"/>: a signed-in user attaches or detaches a provider login.
/// Which account an anonymous external sign-in belongs to is <see cref="IExternalAuthService"/>'s job.
/// </summary>
public sealed class AccountLinkService(
    IUserStore users,
    IEnumerable<IProviderTokenVerifier> verifiers,
    IEventDispatcher events) : IAccountLinkService
{
    public async Task LinkProvider(Guid userId, string provider, string idToken, CancellationToken token = default)
    {
        var verifier = verifiers.FirstOrDefault(v => v.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new BadRequestException($"unsupported-provider:{provider}");

        var identity = await verifier.Verify(idToken, token);

        var existing = await users.FindByProviderKey(identity.Provider, identity.Subject, token);
        if (existing is not null && existing.Id != userId)
            throw new BadRequestException("provider-linked-to-another-account");

        await users.LinkProvider(userId, identity, token);
        await events.PublishAsync(new ProviderLinked { UserId = userId, Provider = identity.Provider }, token);
    }

    public Task UnlinkProvider(Guid userId, string provider, CancellationToken token = default)
        => users.UnlinkProvider(userId, provider, token);
}

namespace Sencilla.Authentication;

/// <summary>
/// The one resolver from an external identity to an account, behind both entry points: <see cref="Exchange"/>
/// verifies a provider <c>id_token</c> (the <c>/external</c> endpoint, verifier selected by provider name)
/// and <see cref="ResolveAccount"/> takes an identity the host already verified (a redirect sign-in).
/// <see cref="IAccountLinkingPolicy"/> decides; this applies the decision to the store.
/// </summary>
public sealed class ExternalAuthService : IExternalAuthService
{
    private readonly IEnumerable<IProviderTokenVerifier> _verifiers;
    private readonly IUserStore _users;
    private readonly IAccountLinkingPolicy _policy;
    private readonly IEventDispatcher _events;

    public ExternalAuthService(
        IEnumerable<IProviderTokenVerifier> verifiers,
        IUserStore users,
        IAccountLinkingPolicy policy,
        IEventDispatcher events)
    {
        _verifiers = verifiers;
        _users = users;
        _policy = policy;
        _events = events;
    }

    public async Task<AuthUser> Exchange(string provider, string idToken, CancellationToken token = default)
    {
        var verifier = _verifiers.FirstOrDefault(v => v.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new BadRequestException($"unsupported-provider:{provider}");

        return await ResolveAccount(await verifier.Verify(idToken, token), token);
    }

    public async Task<AuthUser> ResolveAccount(ExternalIdentity identity, CancellationToken token = default)
    {
        var email = identity.Email?.Trim().ToLowerInvariant();
        var byProvider = await _users.FindByProviderKey(identity.Provider, identity.Subject, token);
        var byEmail = string.IsNullOrEmpty(email) ? null : await _users.FindByEmail(email, token);

        var decision = _policy.Decide(byProvider, byEmail, identity, authenticatedUserId: null);
        var user = decision.Action switch
        {
            LinkAction.Link => await SignInLinkedAccount(byProvider!, identity, token),
            LinkAction.LinkByEmail => await LinkToAccountOwningEmail(byEmail!, identity, token),
            LinkAction.Create => await CreateAccount(email, identity, token),
            _ => throw new UnauthorizedException(decision.Reason ?? "rejected"),
        };

        await _events.PublishAsync(new LoginSucceeded { UserId = user.Id, Method = identity.Provider }, token);
        return user;
    }

    private async Task<AuthUser> SignInLinkedAccount(AuthUser account, ExternalIdentity identity, CancellationToken token)
    {
        if (!identity.EmailVerified || account.EmailConfirmed)
            return account;

        await _users.SetEmailConfirmed(account.Id, token);
        return await _users.FindById(account.Id, token) ?? account;
    }

    private async Task<AuthUser> LinkToAccountOwningEmail(AuthUser account, ExternalIdentity identity, CancellationToken token)
    {
        // A password credential whose address was never confirmed may have been registered by anyone; the
        // provider has now proven who owns the address, so that credential is displaced, not merged with.
        if (!account.EmailConfirmed && await _users.HasPasswordCredential(account.Id, token))
        {
            await _users.RemovePasswordCredential(account.Id, token);
            await _events.PublishAsync(new PasswordChanged { UserId = account.Id, Reason = "removed" }, token);
        }

        await _users.LinkProvider(account.Id, identity, token);
        if (!account.EmailConfirmed)
            await _users.SetEmailConfirmed(account.Id, token);

        await _events.PublishAsync(new ProviderLinked { UserId = account.Id, Provider = identity.Provider }, token);
        return await _users.FindById(account.Id, token) ?? account;
    }

    private async Task<AuthUser> CreateAccount(string? email, ExternalIdentity identity, CancellationToken token)
    {
        var fresh = new AuthUser(Guid.NewGuid(), email, identity.EmailVerified, AuthUserStatus.Active);
        var user = await _users.CreateWithExternalLogin(fresh, identity, token);
        await _events.PublishAsync(new UserRegistered { UserId = user.Id, Email = user.Email, Method = identity.Provider }, token);
        return user;
    }
}

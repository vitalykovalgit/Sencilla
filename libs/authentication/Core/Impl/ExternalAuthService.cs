namespace Sencilla.Authentication;

/// <summary>
/// The one resolver from an external identity to an account, behind both entry points: <see cref="Exchange"/>
/// verifies a provider <c>id_token</c> (the <c>/external</c> endpoint, verifier selected by provider name)
/// and <see cref="ResolveAccount"/> takes an identity the host already verified (a redirect sign-in, or a connect).
/// <see cref="IAccountLinkingPolicy"/> decides; this applies the decision to the store. The recovery service is
/// registered with app accounts only; without it an unverified provider address has no link to go to and is dropped.
/// </summary>
public sealed class ExternalAuthService(
    IEnumerable<IProviderTokenVerifier> verifiers,
    IUserStore users,
    IAccountLinkingPolicy policy,
    IEventDispatcher events,
    IAccountRecoveryService? recovery = null) : IExternalAuthService
{
    public async Task<AuthUser> Exchange(string provider, string idToken, CancellationToken token = default)
    {
        var verifier = verifiers.FirstOrDefault(v => v.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new BadRequestException($"unsupported-provider:{provider}");

        return await ResolveAccount(await verifier.Verify(idToken, token), connectTo: null, token);
    }

    public async Task<AuthUser> ResolveAccount(ExternalIdentity identity, Guid? connectTo = null, CancellationToken token = default)
    {
        var email = identity.Email?.Trim().ToLowerInvariant();
        var byProvider = await users.FindByProviderKey(identity.Provider, identity.Subject, token);
        var byEmail = string.IsNullOrEmpty(email) ? null : await users.FindByEmail(email, token);
        var connecting = connectTo is { } accountId
            ? await users.FindById(accountId, token) ?? throw new UnauthorizedException("user-not-found")
            : null;

        var isProviderOnConnecting = connecting is not null && byProvider is null
            && (await users.GetLinkedProviders(connecting.Id, token)).Contains(identity.Provider, StringComparer.OrdinalIgnoreCase);

        var decision = policy.Decide(byProvider, byEmail, identity, connecting, isProviderOnConnecting);
        var user = decision.Action switch
        {
            LinkAction.Link => await SignInLinkedAccount(byProvider!, email, identity, token),
            LinkAction.Connect => await ConnectToAccount(connecting!, identity, token),
            LinkAction.LinkByEmail => await LinkToAccountOwningEmail(byEmail!, identity, token),
            LinkAction.Create => await CreateAccount(email, identity, token),
            _ => throw new UnauthorizedException(decision.Reason ?? "rejected"),
        };

        await events.PublishAsync(new LoginSucceeded { UserId = user.Id, Method = identity.Provider, ExternalIdentity = identity }, token);
        return user;
    }

    /// <summary>
    /// A known subject signs in. The provider's word confirms the account's address only when it verified that very
    /// address: a different verified address proves nothing about the account's.
    /// </summary>
    private async Task<AuthUser> SignInLinkedAccount(AuthUser account, string? email, ExternalIdentity identity, CancellationToken token)
    {
        var isAccountsAddressVerified = identity.EmailVerified && !string.IsNullOrEmpty(email)
                                        && string.Equals(email, account.Email, StringComparison.OrdinalIgnoreCase);
        if (!isAccountsAddressVerified || account.EmailConfirmed)
            return account;

        await users.SetEmailConfirmed(account.Id, token);
        return await users.FindById(account.Id, token) ?? account;
    }

    private async Task<AuthUser> ConnectToAccount(AuthUser account, ExternalIdentity identity, CancellationToken token)
    {
        await users.LinkProvider(account.Id, identity, token);
        await events.PublishAsync(new ProviderLinked { UserId = account.Id, Provider = identity.Provider }, token);
        return account;
    }

    private async Task<AuthUser> LinkToAccountOwningEmail(AuthUser account, ExternalIdentity identity, CancellationToken token)
    {
        // A password credential whose address was never confirmed may have been registered by anyone; the
        // provider has now proven who owns the address, so that credential is displaced, not merged with.
        if (!account.EmailConfirmed && await users.HasPasswordCredential(account.Id, token))
        {
            await users.RemovePasswordCredential(account.Id, token);
            await events.PublishAsync(new PasswordChanged { UserId = account.Id, Reason = "removed", Provider = identity.Provider }, token);
        }

        await users.LinkProvider(account.Id, identity, token);
        if (!account.EmailConfirmed)
            await users.SetEmailConfirmed(account.Id, token);

        await events.PublishAsync(new ProviderLinked { UserId = account.Id, Provider = identity.Provider }, token);
        return await users.FindById(account.Id, token) ?? account;
    }

    private async Task<AuthUser> CreateAccount(string? email, ExternalIdentity identity, CancellationToken token)
    {
        // An address the provider did not verify is never written to the account: it waits for our own link.
        var verifiedEmail = identity.EmailVerified ? email : null;
        var fresh = new AuthUser(Guid.NewGuid(), verifiedEmail, EmailConfirmed: verifiedEmail is not null, AuthUserStatus.Active);
        var user = await users.CreateWithExternalLogin(fresh, identity, token);
        await events.PublishAsync(new UserRegistered { UserId = user.Id, Email = user.Email, Method = identity.Provider }, token);

        if (verifiedEmail is null && !string.IsNullOrEmpty(email))
            await MailLinkToUnverifiedAddress(user.Id, email, token);
        return user;
    }

    /// <summary>
    /// Mail our own link to the address the provider gave unverified; it reaches the account only when followed. A
    /// refused address (<c>email-invalid</c>, <c>email-taken</c>) leaves the account without one — the sign-in stands.
    /// </summary>
    private async Task MailLinkToUnverifiedAddress(Guid userId, string email, CancellationToken token)
    {
        if (recovery is null)
            return;
        try
        {
            await recovery.RequestEmailVerification(userId, email, token);
        }
        catch (BadRequestException)
        {
        }
    }
}

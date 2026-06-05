namespace Sencilla.Authentication;

/// <summary>
/// Verifies a provider <c>id_token</c>, applies the account-linking policy, and issues tokens.
/// Selects the verifier from the injected set by provider name.
/// </summary>
public sealed class ExternalAuthService : IExternalAuthService
{
    private readonly IEnumerable<IProviderTokenVerifier> _verifiers;
    private readonly IUserStore _users;
    private readonly IAccountLinkingPolicy _policy;
    private readonly IClaimsPrincipalFactory _principals;
    private readonly ITokenIssuer _issuer;
    private readonly IEventDispatcher _events;

    public ExternalAuthService(
        IEnumerable<IProviderTokenVerifier> verifiers,
        IUserStore users,
        IAccountLinkingPolicy policy,
        IClaimsPrincipalFactory principals,
        ITokenIssuer issuer,
        IEventDispatcher events)
    {
        _verifiers = verifiers;
        _users = users;
        _policy = policy;
        _principals = principals;
        _issuer = issuer;
        _events = events;
    }

    public async Task<TokenResponse> Exchange(string provider, string idToken, CancellationToken token = default)
    {
        var verifier = _verifiers.FirstOrDefault(v => v.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new BadRequestException($"unsupported-provider:{provider}");

        var identity = await verifier.Verify(idToken, token);

        var byProvider = await _users.FindByProviderKey(identity.Provider, identity.Subject, token);
        AuthUser? byEmail = null;
        if (identity.EmailVerified && !string.IsNullOrEmpty(identity.Email))
            byEmail = await _users.FindByEmail(identity.Email!.Trim().ToLowerInvariant(), token);

        var decision = _policy.Decide(byProvider, byEmail, identity, authenticatedUserId: null);

        AuthUser user;
        switch (decision.Action)
        {
            case LinkAction.Link:
                user = byProvider!;
                break;

            case LinkAction.Create:
                var fresh = new AuthUser(
                    Guid.NewGuid(),
                    identity.Email?.Trim().ToLowerInvariant(),
                    identity.EmailVerified,
                    AuthUserStatus.Active);
                user = await _users.CreateWithExternalLogin(fresh, identity, token);
                await _events.PublishAsync(new UserRegistered { UserId = user.Id, Email = user.Email, Method = identity.Provider }, token);
                break;

            case LinkAction.Challenge:
                throw new BadRequestException("account-exists-verification-required");

            default:
                throw new UnauthorizedException(decision.Reason ?? "rejected");
        }

        await _events.PublishAsync(new LoginSucceeded { UserId = user.Id, Method = identity.Provider }, token);

        var principal = await _principals.Create(user, scopes: null, token);
        return await _issuer.Issue(principal, token);
    }
}

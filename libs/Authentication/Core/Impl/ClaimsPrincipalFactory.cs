namespace Sencilla.Authentication;

/// <summary>
/// Default principal builder: writes the subject (Guid), email, roles (basic RBAC from the user
/// store) and any requested scopes, then runs the registered <see cref="IClaimsEnricher"/> chain.
/// </summary>
public sealed class ClaimsPrincipalFactory : IClaimsPrincipalFactory
{
    private readonly IUserStore _users;
    private readonly IEnumerable<IClaimsEnricher> _enrichers;

    public ClaimsPrincipalFactory(IUserStore users, IEnumerable<IClaimsEnricher> enrichers)
    {
        _users = users;
        _enrichers = enrichers;
    }

    public async Task<ClaimsPrincipal> Create(AuthUser user, IEnumerable<string>? scopes = null, CancellationToken token = default)
    {
        var claims = new List<Claim> { new(AuthClaims.Subject, user.Id.ToString()) };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(AuthClaims.Email, user.Email));
            claims.Add(new Claim(AuthClaims.EmailVerified, user.EmailConfirmed ? "true" : "false"));
        }

        foreach (var role in await _users.GetRoles(user.Id, token))
            claims.Add(new Claim(AuthClaims.Roles, role));

        if (scopes is not null)
            foreach (var scope in scopes)
                claims.Add(new Claim("scope", scope));

        foreach (var enricher in _enrichers)
            await enricher.Enrich(claims, user, token);

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Sencilla", AuthClaims.Subject, AuthClaims.Roles));
    }
}

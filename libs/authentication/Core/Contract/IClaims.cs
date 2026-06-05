namespace Sencilla.Authentication;

/// <summary>
/// Builds the <see cref="ClaimsPrincipal"/> for an authenticated user (subject = Guid, email,
/// requested scopes) and runs the registered <see cref="IClaimsEnricher"/> chain. This is the
/// shared engine seam: the OIDC host hands the principal to its sign-in pipeline, while the
/// token-API path passes it to an <see cref="ITokenIssuer"/>.
/// </summary>
public interface IClaimsPrincipalFactory
{
    Task<ClaimsPrincipal> Create(AuthUser user, IEnumerable<string>? scopes = null, CancellationToken token = default);
}

/// <summary>
/// Optional enrichment applied while building the principal — e.g. the Security component adds
/// role / scope claims. Multiple enrichers run in sequence.
/// </summary>
public interface IClaimsEnricher
{
    Task Enrich(ICollection<Claim> claims, AuthUser user, CancellationToken token = default);
}

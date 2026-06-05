namespace Sencilla.Authentication;

/// <summary>
/// Verifies a provider-issued <c>id_token</c> on the exchange path and normalizes it to an
/// <see cref="ExternalIdentity"/>. Each provider package contributes one implementation; the
/// orchestration layer selects by <see cref="Provider"/> from the injected set.
/// </summary>
public interface IProviderTokenVerifier
{
    /// <summary>Provider key, e.g. <c>google</c>, <c>apple</c>, <c>facebook</c>.</summary>
    string Provider { get; }

    /// <summary>
    /// Validate signature, issuer, audience (against the configured set) and lifetime, then map
    /// the token's claims to an <see cref="ExternalIdentity"/>. Throws
    /// <see cref="UnauthorizedException"/> on any validation failure.
    /// </summary>
    Task<ExternalIdentity> Verify(string idToken, CancellationToken token = default);
}

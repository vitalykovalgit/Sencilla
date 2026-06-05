namespace Sencilla.Authentication;

/// <summary>
/// Normalized identity returned by an <see cref="IProviderTokenVerifier"/> after validating a
/// provider <c>id_token</c>. <see cref="Subject"/> is the provider's stable subject (never the
/// email) and is the key the linking policy uses, so a missing/relay <see cref="Email"/> is safe.
/// </summary>
public sealed record ExternalIdentity(
    string Provider,
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name = null,
    string? Picture = null,
    IReadOnlyDictionary<string, string>? RawClaims = null);

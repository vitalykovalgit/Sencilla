namespace Sencilla.Authentication;

/// <summary>
/// Normalized identity returned by an <see cref="IProviderTokenVerifier"/> after validating a
/// provider <c>id_token</c>. <see cref="Subject"/> is the provider's stable subject (never the
/// email) and is the key the linking policy uses, so a missing/relay <see cref="Email"/> is safe.
/// <see cref="GivenName"/> / <see cref="FamilyName"/> are the two parts as the provider sent them; a provider
/// that sends only the display <see cref="Name"/> leaves both null.
/// </summary>
public sealed record ExternalIdentity(
    string Provider,
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name = null,
    string? Picture = null,
    IReadOnlyDictionary<string, string>? RawClaims = null,
    string? GivenName = null,
    string? FamilyName = null);

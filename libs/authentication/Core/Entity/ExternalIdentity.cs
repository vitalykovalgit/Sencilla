namespace Sencilla.Authentication;

/// <summary>
/// Normalized identity returned by an <see cref="IProviderTokenVerifier"/> after validating a
/// provider <c>id_token</c>. <see cref="Subject"/> is the provider's stable subject (never the
/// email) and is the key the linking policy uses, so a missing/relay <see cref="Email"/> is safe.
/// <see cref="GivenName"/> / <see cref="FamilyName"/> are the two parts as the provider sent them; a provider
/// that sends only the display <see cref="Name"/> leaves both null.
/// <see cref="Phone"/> / <see cref="PhoneVerified"/> come only from a provider that shares a phone (Telegram); a
/// verified phone is a confirmed contact — of a new account, or of an account the provider is attached to whose
/// number it is (or that has none) — and never written over a phone the user edited.
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
    string? FamilyName = null,
    long? Phone = null,
    bool PhoneVerified = false);

namespace Sencilla.Authentication;

/// <summary>
/// The user plus their stored password hash, fetched in a single round-trip so the hash never
/// needs to be exposed on <see cref="AuthUser"/>.
/// </summary>
public sealed record PasswordCredential(AuthUser User, string PasswordHash);

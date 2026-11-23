namespace Sencilla.Authentication;

/// <summary>
/// Password registration, login, and change-password. Register and login return the authenticated
/// user; the endpoint starts the session through the <see cref="ISessionTransport"/>.
/// </summary>
public interface ICredentialAuthService
{
    Task<AuthUser> Register(RegisterRequest request, CancellationToken token = default);

    Task<AuthUser> Login(LoginRequest request, CancellationToken token = default);

    Task ChangePassword(Guid userId, string currentPassword, string newPassword, CancellationToken token = default);
}

/// <summary>
/// The one resolver from an external identity to the account it belongs to, applying
/// <see cref="IAccountLinkingPolicy"/>: a known subject signs into its account, a provider-verified
/// address links to the account that owns it, a fresh address provisions an account, anything else is
/// refused with <see cref="UnauthorizedException"/>.
/// </summary>
public interface IExternalAuthService
{
    /// <summary>Verify a provider <c>id_token</c> and resolve the account it belongs to.</summary>
    Task<AuthUser> Exchange(string provider, string idToken, CancellationToken token = default);

    /// <summary>
    /// Resolve the account of an identity the caller has ALREADY verified (a host-side redirect sign-in). With
    /// <paramref name="connectTo"/> — the signed-in account a connect started from — the provider is attached to that
    /// account instead, refused <c>provider-linked-to-another-account</c> when the identity is another account's,
    /// <c>provider-already-connected</c> when the account already has a login of that provider under another subject,
    /// and <c>confirm-email-first</c> while the account's own address is unconfirmed. An address the provider did not
    /// verify never becomes an account's: a new account gets none, and our own link goes to it as a pending address.
    /// </summary>
    Task<AuthUser> ResolveAccount(ExternalIdentity identity, Guid? connectTo = null, CancellationToken token = default);
}

/// <summary>Email verification and password reset (single-use, hashed tokens).</summary>
public interface IAccountRecoveryService
{
    /// <summary>
    /// Mail a verification link. With no <paramref name="email"/>: to the account's own unconfirmed address, or —
    /// for an account with no address — again to its pending one. With one typed for an account that has no
    /// address: to it, as the pending address (replacing an earlier one), which reaches the account only when the
    /// link is followed. Refuses <c>email-invalid</c>, <c>email-already-set</c> (the account has another address)
    /// and <c>email-taken</c> (another account has it).
    /// </summary>
    Task RequestEmailVerification(Guid userId, string? email = null, CancellationToken token = default);

    /// <summary>The address the user's open verification link proves, while it is not yet the account's (an expired link still counts); null when none.</summary>
    Task<string?> FindPendingEmail(Guid userId, CancellationToken token = default);

    /// <summary>
    /// Consume a verification token and mark the address confirmed — for a pending address, put it on the
    /// account, or refuse <c>email-taken</c> when another account took it meanwhile. Returns the user.
    /// </summary>
    Task<AuthUser> ConfirmEmail(string verificationToken, CancellationToken token = default);

    Task RequestPasswordReset(string email, CancellationToken token = default);

    /// <summary>Consume a reset token, set the new password, revoke other sessions. Returns the user so the endpoint can sign them in.</summary>
    Task<AuthUser> ResetPassword(string resetToken, string newPassword, CancellationToken token = default);
}

/// <summary>Link / unlink an external provider to an authenticated account.</summary>
public interface IAccountLinkService
{
    Task LinkProvider(Guid userId, string provider, string idToken, CancellationToken token = default);

    /// <summary>Remove the provider's logins from the account; refuses <c>last-sign-in-method</c> while it is the only way in (no other provider, no password).</summary>
    Task UnlinkProvider(Guid userId, string provider, CancellationToken token = default);
}

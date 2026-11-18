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

    /// <summary>Resolve the account of an identity the caller has ALREADY verified (a host-side redirect sign-in).</summary>
    Task<AuthUser> ResolveAccount(ExternalIdentity identity, CancellationToken token = default);
}

/// <summary>Email verification and password reset (single-use, hashed tokens).</summary>
public interface IAccountRecoveryService
{
    Task RequestEmailVerification(Guid userId, CancellationToken token = default);

    /// <summary>Consume a verification token and mark the address confirmed. Returns the user.</summary>
    Task<AuthUser> ConfirmEmail(string verificationToken, CancellationToken token = default);

    Task RequestPasswordReset(string email, CancellationToken token = default);

    /// <summary>Consume a reset token, set the new password, revoke other sessions. Returns the user so the endpoint can sign them in.</summary>
    Task<AuthUser> ResetPassword(string resetToken, string newPassword, CancellationToken token = default);
}

/// <summary>Link / unlink an external provider to an authenticated account.</summary>
public interface IAccountLinkService
{
    Task LinkProvider(Guid userId, string provider, string idToken, CancellationToken token = default);

    Task UnlinkProvider(Guid userId, string provider, CancellationToken token = default);
}

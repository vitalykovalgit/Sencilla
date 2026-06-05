namespace Sencilla.Authentication;

/// <summary>Password registration, login, and change-password. Implemented in Server.</summary>
public interface ICredentialAuthService
{
    Task<TokenResponse> Register(RegisterRequest request, CancellationToken token = default);

    Task<TokenResponse> Login(LoginRequest request, CancellationToken token = default);

    Task ChangePassword(Guid userId, string currentPassword, string newPassword, CancellationToken token = default);
}

/// <summary>Verify a provider <c>id_token</c>, apply the linking policy, and issue tokens.</summary>
public interface IExternalAuthService
{
    Task<TokenResponse> Exchange(string provider, string idToken, CancellationToken token = default);
}

/// <summary>Email verification and password reset (single-use, hashed tokens).</summary>
public interface IAccountRecoveryService
{
    Task RequestEmailVerification(Guid userId, CancellationToken token = default);

    Task ConfirmEmail(string verificationToken, CancellationToken token = default);

    Task RequestPasswordReset(string email, CancellationToken token = default);

    Task ResetPassword(string resetToken, string newPassword, CancellationToken token = default);
}

/// <summary>Link / unlink an external provider to an authenticated account.</summary>
public interface IAccountLinkService
{
    Task LinkProvider(Guid userId, string provider, string idToken, CancellationToken token = default);

    Task UnlinkProvider(Guid userId, string provider, CancellationToken token = default);
}

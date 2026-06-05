namespace Sencilla.Authentication;

/// <summary>
/// The user + credential store the authentication family depends on. The default implementation
/// (<c>Sencilla.Authentication.Users</c>) bridges to <c>Sencilla.Component.Users</c>; any project
/// may supply its own. Multi-write signup goes through the coarse <see cref="CreateWithPasswordCredential"/>
/// / <see cref="CreateWithExternalLogin"/> operations, which the implementation makes atomic so the
/// orchestration layer never spans a transaction itself.
/// </summary>
public interface IUserStore
{
    Task<AuthUser?> FindById(Guid id, CancellationToken token = default);

    Task<AuthUser?> FindByEmail(string email, CancellationToken token = default);

    /// <summary>Resolve a user by phone number — backs phone sign-in (<c>AllowPhoneLogin</c>).</summary>
    Task<AuthUser?> FindByPhone(long phone, CancellationToken token = default);

    Task<AuthUser?> FindByProviderKey(string provider, string providerKey, CancellationToken token = default);

    /// <summary>User + password hash for the <c>password</c> provider row, in one round-trip.</summary>
    Task<PasswordCredential?> FindPasswordCredential(string email, CancellationToken token = default);

    /// <summary>Atomically create the user and its <c>password</c> credential row.</summary>
    Task<AuthUser> CreateWithPasswordCredential(AuthUser user, string passwordHash, CancellationToken token = default);

    /// <summary>Atomically create the user and an external-login row from a verified identity.</summary>
    Task<AuthUser> CreateWithExternalLogin(AuthUser user, ExternalIdentity identity, CancellationToken token = default);

    Task SetPasswordHash(Guid userId, string passwordHash, CancellationToken token = default);

    Task LinkProvider(Guid userId, ExternalIdentity identity, CancellationToken token = default);

    Task SetEmailConfirmed(Guid userId, CancellationToken token = default);

    Task<IReadOnlyList<string>> GetRoles(Guid userId, CancellationToken token = default);
}

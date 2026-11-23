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

    /// <summary>Resolve a user by phone number — backs phone sign-in (<c>AppAccountsOptions.PhoneLoginEnabled</c>).</summary>
    Task<AuthUser?> FindByPhone(long phone, CancellationToken token = default);

    Task<AuthUser?> FindByProviderKey(string provider, string providerKey, CancellationToken token = default);

    /// <summary>User + password hash for the <c>password</c> provider row, in one round-trip.</summary>
    Task<PasswordCredential?> FindPasswordCredential(string email, CancellationToken token = default);

    /// <summary>Atomically create the user and its <c>password</c> credential row.</summary>
    Task<AuthUser> CreateWithPasswordCredential(AuthUser user, string passwordHash, CancellationToken token = default);

    /// <summary>Atomically create the user and an external-login row from a verified identity.</summary>
    Task<AuthUser> CreateWithExternalLogin(AuthUser user, ExternalIdentity identity, CancellationToken token = default);

    Task SetPasswordHash(Guid userId, string passwordHash, CancellationToken token = default);

    /// <summary>
    /// Attach a provider login to an existing account. A provider-verified phone confirms the account's number when it
    /// is that number, or is written, confirmed, when the account has none — never over a different one, and not
    /// confirmed while another account has it confirmed.
    /// </summary>
    Task LinkProvider(Guid userId, ExternalIdentity identity, CancellationToken token = default);

    Task SetEmailConfirmed(Guid userId, CancellationToken token = default);

    /// <summary>
    /// Put an address the user has just proven (a followed verification link) on the account, confirmed.
    /// Refuses with <c>email-taken</c> when another account holds it by then.
    /// </summary>
    Task SetConfirmedEmail(Guid userId, string email, CancellationToken token = default);

    Task<bool> HasPasswordCredential(Guid userId, CancellationToken token = default);

    /// <summary>Delete the <c>password</c> credential row (bumps <see cref="AuthUser.CredentialsChangedAt"/>).</summary>
    Task RemovePasswordCredential(Guid userId, CancellationToken token = default);

    /// <summary>Delete the external-login rows of one provider.</summary>
    Task UnlinkProvider(Guid userId, string provider, CancellationToken token = default);

    /// <summary>The providers the account signs in with, lower-cased, each once — a password is not one (<see cref="HasPasswordCredential"/>).</summary>
    Task<IReadOnlyList<string>> GetLinkedProviders(Guid userId, CancellationToken token = default);

    /// <summary>Whether the account's phone is confirmed and another live account has the same number (typed, as a confirmed phone belongs to one account).</summary>
    Task<bool> IsConfirmedPhoneOnAnotherAccount(Guid userId, CancellationToken token = default);

    Task<IReadOnlyList<string>> GetRoles(Guid userId, CancellationToken token = default);
}

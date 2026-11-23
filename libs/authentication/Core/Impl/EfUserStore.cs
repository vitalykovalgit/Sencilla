namespace Sencilla.Authentication;

/// <summary>
/// Default <see cref="IUserStore"/> over the <c>Sencilla.Component.Users</c> entities. The entities
/// are <c>IEntity&lt;Guid&gt;</c>, so the two-parameter repository interfaces are used throughout.
/// Every operation runs under <see cref="Access.Root"/>: the caller of register / login / reset is
/// anonymous, and these rows are owned by the framework, not by the security matrix. Reads are
/// non-tracking on purpose: a user found by <see cref="FindByEmail"/> is routinely updated a few
/// calls later in the same request, and a tracked copy would make that update throw. This part holds the account rows;
/// <c>EfUserStore.Logins.cs</c> the login rows (password, providers), <c>EfUserStore.Phone.cs</c> the confirmed phone.
/// </summary>
public sealed partial class EfUserStore : IUserStore
{
    private const string PasswordProvider = "password";

    private readonly IReadRepository<User, Guid> _userRead;
    private readonly ICreateRepository<User, Guid> _userCreate;
    private readonly IUpdateRepository<User, Guid> _userUpdate;
    private readonly IReadRepository<UserAuth, Guid> _authRead;
    private readonly ICreateRepository<UserAuth, Guid> _authCreate;
    private readonly IUpdateRepository<UserAuth, Guid> _authUpdate;
    private readonly IDeleteRepository<UserAuth, Guid> _authDelete;
    private readonly IReadRepository<UserRole, Guid> _roleRead;

    public EfUserStore(
        IReadRepository<User, Guid> userRead,
        ICreateRepository<User, Guid> userCreate,
        IUpdateRepository<User, Guid> userUpdate,
        IReadRepository<UserAuth, Guid> authRead,
        ICreateRepository<UserAuth, Guid> authCreate,
        IUpdateRepository<UserAuth, Guid> authUpdate,
        IDeleteRepository<UserAuth, Guid> authDelete,
        IReadRepository<UserRole, Guid> roleRead)
    {
        _userRead = userRead;
        _userCreate = userCreate;
        _userUpdate = userUpdate;
        _authRead = authRead;
        _authCreate = authCreate;
        _authUpdate = authUpdate;
        _authDelete = authDelete;
        _roleRead = roleRead;
    }

    public async Task<AuthUser?> FindById(Guid id, CancellationToken token = default)
    {
        using var _ = Access.Root();
        return await _userRead.GetById(id, token) is { } user ? Map(user) : null;
    }

    public async Task<AuthUser?> FindByEmail(string email, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var normalized = Normalize(email);
        var user = await _userRead.Query.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized, token);
        return user is null ? null : Map(user);
    }

    public async Task<AuthUser?> FindByPhone(long phone, CancellationToken token = default)
    {
        if (phone == 0)
            return null;
        using var _ = Access.Root();
        var user = await _userRead.Query.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone, token);
        return user is null ? null : Map(user);
    }

    public async Task<AuthUser?> FindByProviderKey(string provider, string providerKey, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var key = provider.ToLowerInvariant();
        // Provider keys are compared case-insensitively: rows written by the legacy first-login path
        // carry the ASP.NET scheme name ("Google"), the token verifiers use "google".
        var auth = await _authRead.Query.AsNoTracking().FirstOrDefaultAsync(a => a.Auth.ToLower() == key && a.ProviderKey == providerKey, token);
        return auth is null ? null : await FindById(auth.UserId, token);
    }

    public async Task<AuthUser> CreateWithPasswordCredential(AuthUser user, string passwordHash, CancellationToken token = default)
    {
        using var _ = Access.Root();
        await using var transaction = await _userRead.BeginTransaction(token);

        var created = await CreateUser(user, token);
        await _authCreate.Create(
            NewAuth(created.Id, PasswordProvider, Normalize(created.Email), created.Email, passwordHash), token);

        await transaction.CommitAsync(token);
        return Map(created);
    }

    public async Task<AuthUser> CreateWithExternalLogin(AuthUser user, ExternalIdentity identity, CancellationToken token = default)
    {
        using var _ = Access.Root();
        await using var transaction = await _userRead.BeginTransaction(token);

        var created = await CreateUser(user, token, identity);
        await _authCreate.Create(
            NewAuth(created.Id, identity.Provider, identity.Subject, identity.Email), token);

        await transaction.CommitAsync(token);
        return Map(created);
    }

    public async Task SetEmailConfirmed(Guid userId, CancellationToken token = default)
    {
        using var _ = Access.Root();
        if (await _userRead.GetById(userId, token) is not { } user)
            return;
        user.EmailConf = true;
        await _userUpdate.Update(user, token);
    }

    public async Task SetConfirmedEmail(Guid userId, string email, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var normalized = Normalize(email);
        // The unique index UX_User_Email backs this check against a write landing between the two.
        if (await _userRead.Query.AsNoTracking().AnyAsync(u => u.Email == normalized && u.Id != userId, token))
            throw new BadRequestException("email-taken");
        if (await _userRead.GetById(userId, token) is not { } user)
            return;
        user.Email = normalized;
        user.EmailConf = true;
        await _userUpdate.Update(user, token);
    }

    public async Task<IReadOnlyList<string>> GetRoles(Guid userId, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var roles = await _roleRead.Query.AsNoTracking().Where(r => r.UserId == userId).ToListAsync(token);
        return roles.Select(r => r.Role ?? r.RoleId.ToString()).ToList();
    }

    private async Task<User> CreateUser(AuthUser user, CancellationToken token, ExternalIdentity? identity = null)
    {
        var (first, last) = FirstAndLastNameOf(identity);
        var id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id;
        var (phone, isPhoneConfirmed) = await PhoneWithProvidersNumber(id, identity?.Phone ?? user.Phone ?? 0, isConfirmed: false, identity, token);
        var entity = new User
        {
            Id = id,
            Email = user.Email,
            EmailConf = user.EmailConfirmed,
            Phone = phone,
            PhoneConf = isPhoneConfirmed,
            FirstName = first,
            LastName = last,
            Pic = identity?.Picture,
            Status = (byte)UserStatuses.Registered,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
        };
        return await _userCreate.Create(entity, token) ?? entity;
    }

    /// <summary>
    /// The provider's given and family names as it sent them. A provider that sends only a display name
    /// gets it split — first word, rest — which cuts a two-word given name, so it is only the fallback.
    /// </summary>
    private static (string? First, string? Last) FirstAndLastNameOf(ExternalIdentity? identity)
    {
        if (identity is { GivenName: not null } or { FamilyName: not null })
            return (identity.GivenName, identity.FamilyName);
        var trimmed = identity?.Name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return (null, null);
        var space = trimmed.IndexOf(' ');
        return space < 0 ? (trimmed, null) : (trimmed[..space], trimmed[(space + 1)..].Trim());
    }

    private static AuthUser Map(User user) => new(
        user.Id,
        user.Email,
        user.EmailConf,
        user.DeletedDate is null ? AuthUserStatus.Active : AuthUserStatus.Disabled,
        user.Phone == 0 ? null : user.Phone,
        user.CredentialsChangedAt);

    private static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}

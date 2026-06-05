namespace Sencilla.Authentication;

/// <summary>
/// Default <see cref="IUserStore"/> over the <c>Sencilla.Component.Users</c> entities. The entities
/// are <c>IEntity&lt;Guid&gt;</c>, so the two-parameter repository interfaces are used throughout.
/// </summary>
public sealed class EfUserStore : IUserStore
{
    private const string PasswordProvider = "password";

    private readonly IReadRepository<User, Guid> _userRead;
    private readonly ICreateRepository<User, Guid> _userCreate;
    private readonly IUpdateRepository<User, Guid> _userUpdate;
    private readonly IReadRepository<UserAuth, Guid> _authRead;
    private readonly ICreateRepository<UserAuth, Guid> _authCreate;
    private readonly IUpdateRepository<UserAuth, Guid> _authUpdate;
    private readonly IReadRepository<UserRole, Guid> _roleRead;

    public EfUserStore(
        IReadRepository<User, Guid> userRead,
        ICreateRepository<User, Guid> userCreate,
        IUpdateRepository<User, Guid> userUpdate,
        IReadRepository<UserAuth, Guid> authRead,
        ICreateRepository<UserAuth, Guid> authCreate,
        IUpdateRepository<UserAuth, Guid> authUpdate,
        IReadRepository<UserRole, Guid> roleRead)
    {
        _userRead = userRead;
        _userCreate = userCreate;
        _userUpdate = userUpdate;
        _authRead = authRead;
        _authCreate = authCreate;
        _authUpdate = authUpdate;
        _roleRead = roleRead;
    }

    public async Task<AuthUser?> FindById(Guid id, CancellationToken token = default)
        => await _userRead.GetById(id, token) is { } user ? Map(user) : null;

    public async Task<AuthUser?> FindByEmail(string email, CancellationToken token = default)
    {
        var normalized = Normalize(email);
        var user = await _userRead.Query.FirstOrDefaultAsync(u => u.Email == normalized, token);
        return user is null ? null : Map(user);
    }

    public async Task<AuthUser?> FindByPhone(long phone, CancellationToken token = default)
    {
        if (phone == 0)
            return null;
        var user = await _userRead.Query.FirstOrDefaultAsync(u => u.Phone == phone, token);
        return user is null ? null : Map(user);
    }

    public async Task<AuthUser?> FindByProviderKey(string provider, string providerKey, CancellationToken token = default)
    {
        var auth = await _authRead.Query.FirstOrDefaultAsync(a => a.Auth == provider && a.ProviderKey == providerKey, token);
        return auth is null ? null : await FindById(auth.UserId, token);
    }

    public async Task<PasswordCredential?> FindPasswordCredential(string email, CancellationToken token = default)
    {
        var normalized = Normalize(email);
        var auth = await _authRead.Query
            .FirstOrDefaultAsync(a => a.Auth == PasswordProvider && a.ProviderKey == normalized, token);
        if (auth?.PasswordHash is null)
            return null;

        var user = await _userRead.GetById(auth.UserId, token);
        return user is null ? null : new PasswordCredential(Map(user), auth.PasswordHash);
    }

    public async Task<AuthUser> CreateWithPasswordCredential(AuthUser user, string passwordHash, CancellationToken token = default)
    {
        await using var transaction = await _userRead.BeginTransaction(token);

        var created = await CreateUser(user, token);
        await _authCreate.Create(
            NewAuth(created.Id, PasswordProvider, Normalize(created.Email), created.Email, passwordHash), token);

        await transaction.CommitAsync(token);
        return Map(created);
    }

    public async Task<AuthUser> CreateWithExternalLogin(AuthUser user, ExternalIdentity identity, CancellationToken token = default)
    {
        await using var transaction = await _userRead.BeginTransaction(token);

        var created = await CreateUser(user, token);
        await _authCreate.Create(
            NewAuth(created.Id, identity.Provider, identity.Subject, identity.Email), token);

        await transaction.CommitAsync(token);
        return Map(created);
    }

    public async Task SetPasswordHash(Guid userId, string passwordHash, CancellationToken token = default)
    {
        var auth = await _authRead.Query
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Auth == PasswordProvider, token);

        if (auth is null)
        {
            var user = await _userRead.GetById(userId, token);
            await _authCreate.Create(
                NewAuth(userId, PasswordProvider, Normalize(user?.Email), user?.Email, passwordHash), token);
        }
        else
        {
            auth.PasswordHash = passwordHash;
            await _authUpdate.Update(auth, token);
        }
    }

    public async Task LinkProvider(Guid userId, ExternalIdentity identity, CancellationToken token = default)
        => await _authCreate.Create(
            NewAuth(userId, identity.Provider, identity.Subject, identity.Email), token);

    public async Task SetEmailConfirmed(Guid userId, CancellationToken token = default)
    {
        if (await _userRead.GetById(userId, token) is not { } user)
            return;
        user.EmailConf = true;
        await _userUpdate.Update(user, token);
    }

    public async Task<IReadOnlyList<string>> GetRoles(Guid userId, CancellationToken token = default)
    {
        var roles = await _roleRead.Query.Where(r => r.UserId == userId).ToListAsync(token);
        return roles.Select(r => r.Role ?? r.RoleId.ToString()).ToList();
    }

    private async Task<User> CreateUser(AuthUser user, CancellationToken token)
    {
        var entity = new User
        {
            Id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id,
            Email = user.Email,
            EmailConf = user.EmailConfirmed,
            Phone = user.Phone ?? 0,
            Status = (byte)UserStatuses.Registered,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
        };
        return await _userCreate.Create(entity, token) ?? entity;
    }

    private static UserAuth NewAuth(Guid userId, string auth, string? providerKey, string? email, string? passwordHash = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Auth = auth,
        ProviderKey = providerKey,
        Email = email,
        PasswordHash = passwordHash,
    };

    private static AuthUser Map(User user) => new(
        user.Id,
        user.Email,
        user.EmailConf,
        user.DeletedDate is null ? AuthUserStatus.Active : AuthUserStatus.Disabled,
        user.Phone == 0 ? null : user.Phone);

    private static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}

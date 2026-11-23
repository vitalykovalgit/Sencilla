namespace Sencilla.Authentication;

/// <summary>The login rows of an account: its password credential and the providers it signs in with.</summary>
public sealed partial class EfUserStore
{
    public async Task<PasswordCredential?> FindPasswordCredential(string email, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var normalized = Normalize(email);
        var auth = await _authRead.Query.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Auth == PasswordProvider && a.ProviderKey == normalized, token);
        if (auth?.PasswordHash is null)
            return null;

        var user = await _userRead.GetById(auth.UserId, token);
        return user is null ? null : new PasswordCredential(Map(user), auth.PasswordHash);
    }

    public async Task SetPasswordHash(Guid userId, string passwordHash, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var auth = await _authRead.Query.AsNoTracking()
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

        await TouchCredentials(userId, token);
    }

    public async Task<bool> HasPasswordCredential(Guid userId, CancellationToken token = default)
    {
        using var _ = Access.Root();
        return await _authRead.Query.AsNoTracking().AnyAsync(a => a.UserId == userId && a.Auth == PasswordProvider && a.PasswordHash != null, token);
    }

    public async Task RemovePasswordCredential(Guid userId, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var rows = await _authRead.Query.AsNoTracking().Where(a => a.UserId == userId && a.Auth == PasswordProvider).ToListAsync(token);
        if (rows.Count == 0)
            return;

        await _authDelete.Delete(rows, token);
        await TouchCredentials(userId, token);
    }

    public async Task LinkProvider(Guid userId, ExternalIdentity identity, CancellationToken token = default)
    {
        using var _ = Access.Root();
        await WriteProvidersPhone(userId, identity, token);

        var key = identity.Provider.ToLowerInvariant();
        var rows = await _authRead.Query.AsNoTracking().Where(a => a.UserId == userId && a.Auth.ToLower() == key).ToListAsync(token);

        if (rows.Any(a => a.ProviderKey == identity.Subject))
            return;

        // Adopt a row the legacy first-login path wrote without a subject (Auth = scheme name,
        // ProviderKey NULL) instead of adding a second one for the same provider.
        var legacy = rows.FirstOrDefault(a => a.ProviderKey is null);
        if (legacy is not null)
        {
            legacy.ProviderKey = identity.Subject;
            legacy.Email ??= identity.Email;
            await _authUpdate.Update(legacy, token);
            return;
        }

        await _authCreate.Create(NewAuth(userId, identity.Provider, identity.Subject, identity.Email), token);
    }

    public async Task UnlinkProvider(Guid userId, string provider, CancellationToken token = default)
    {
        using var _ = Access.Root();
        var key = provider.ToLowerInvariant();
        if (key == PasswordProvider)
            return;
        var rows = await _authRead.Query.AsNoTracking().Where(a => a.UserId == userId && a.Auth.ToLower() == key).ToListAsync(token);
        if (rows.Count > 0)
            await _authDelete.Delete(rows, token);
    }

    public async Task<IReadOnlyList<string>> GetLinkedProviders(Guid userId, CancellationToken token = default)
    {
        using var _ = Access.Root();
        // Lower-cased: rows of the legacy first-login path carry the ASP.NET scheme name ("Google").
        return await _authRead.Query.AsNoTracking()
            .Where(a => a.UserId == userId && a.Auth != PasswordProvider)
            .Select(a => a.Auth.ToLower())
            .Distinct()
            .ToListAsync(token);
    }

    /// <summary>Stamp the user so sessions issued before this moment can be rejected (see the cookie validator).</summary>
    private async Task TouchCredentials(Guid userId, CancellationToken token)
    {
        if (await _userRead.GetById(userId, token) is not { } user)
            return;
        user.CredentialsChangedAt = DateTime.UtcNow;
        await _userUpdate.Update(user, token);
    }

    private static UserAuth NewAuth(Guid userId, string auth, string? providerKey, string? email, string? passwordHash = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Auth = auth,
        ProviderKey = providerKey,
        Email = email,
        PasswordHash = passwordHash,
        CreatedDate = DateTime.UtcNow,
    };
}

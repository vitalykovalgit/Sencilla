namespace Sencilla.Authentication;

/// <summary>
/// SQL-backed <see cref="IRefreshTokenStore"/> over <c>sec.RefreshToken</c>. Selected via
/// <c>UseJwtToken(o =&gt; o.Refresh.Storage = RefreshStorage.EntityFramework)</c>, replacing the default
/// <see cref="InMemoryRefreshTokenStore"/>. Rows are keyed by the SHA-256 of the token, so a database
/// read never yields a usable session; the raw token exists only on the client.
/// </summary>
public sealed class DbRefreshTokenStore(
    ICreateRepository<RefreshTokenEntity, string> create,
    IUpdateRepository<RefreshTokenEntity, string> update) : IRefreshTokenStore
{
    public async Task Add(RefreshToken token, CancellationToken cancellation = default)
    {
        using var _ = Access.Root();
        await create.Create(Map(token), cancellation);
    }

    public async Task<RefreshToken?> Find(string token, CancellationToken cancellation = default)
    {
        using var _ = Access.Root();
        var entity = await create.GetById(Hash(token), cancellation);
        return entity is null ? null : Map(entity, token);
    }

    public async Task Update(RefreshToken token, CancellationToken cancellation = default)
    {
        using var _ = Access.Root();
        var entity = await create.GetById(Hash(token.Token), cancellation);
        if (entity is null)
            return;

        entity.RedeemedAt = token.RedeemedAt;
        entity.Revoked = token.Revoked;
        await update.Update(entity, cancellation);
    }

    public Task RevokeFamily(Guid familyId, CancellationToken cancellation = default)
    {
        using var _ = Access.Root();
        return create.Query
            .Where(t => t.FamilyId == familyId && !t.Revoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true), cancellation);
    }

    public Task RevokeUser(Guid userId, CancellationToken cancellation = default)
    {
        using var _ = Access.Root();
        return create.Query
            .Where(t => t.UserId == userId && !t.Revoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true), cancellation);
    }

    /// <summary>Lower-case hex SHA-256 — 64 chars, well inside the 128-char key column.</summary>
    public static string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    private static RefreshTokenEntity Map(RefreshToken t) => new()
    {
        Id = Hash(t.Token),
        UserId = t.UserId,
        FamilyId = t.FamilyId,
        ExpiresAt = t.ExpiresAt,
        RedeemedAt = t.RedeemedAt,
        Revoked = t.Revoked,
    };

    private static RefreshToken Map(RefreshTokenEntity e, string rawToken) => new()
    {
        Token = rawToken,
        UserId = e.UserId,
        FamilyId = e.FamilyId,
        ExpiresAt = e.ExpiresAt,
        RedeemedAt = e.RedeemedAt,
        Revoked = e.Revoked,
    };
}

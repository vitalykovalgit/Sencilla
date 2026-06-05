namespace Sencilla.Authentication;

/// <summary>
/// SQL-backed <see cref="IRefreshTokenStore"/> over <c>sec.RefreshToken</c>. Selected via
/// <c>UseJwtToken(o =&gt; o.RefreshToken(r =&gt; r.PersistEntityFramework()))</c>, replacing the default
/// <see cref="InMemoryRefreshTokenStore"/>.
/// </summary>
public sealed class DbRefreshTokenStore(
    ICreateRepository<RefreshTokenEntity, string> create,
    IUpdateRepository<RefreshTokenEntity, string> update) : IRefreshTokenStore
{
    public async Task Add(RefreshToken token, CancellationToken cancellation = default)
    {
        await create.Create(Map(token), cancellation);
    }

    public async Task<RefreshToken?> Find(string token, CancellationToken cancellation = default)
    {
        var entity = await create.GetById(token, cancellation);
        return entity is null ? null : Map(entity);
    }

    public async Task Update(RefreshToken token, CancellationToken cancellation = default)
    {
        var entity = await create.GetById(token.Token, cancellation);
        if (entity is null)
            return;

        entity.RedeemedAt = token.RedeemedAt;
        entity.Revoked = token.Revoked;
        await update.Update(entity, cancellation);
    }

    public Task RevokeFamily(Guid familyId, CancellationToken cancellation = default)
        => create.Query
            .Where(t => t.FamilyId == familyId && !t.Revoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true), cancellation);

    private static RefreshTokenEntity Map(RefreshToken t) => new()
    {
        Id = t.Token,
        UserId = t.UserId,
        FamilyId = t.FamilyId,
        ExpiresAt = t.ExpiresAt,
        RedeemedAt = t.RedeemedAt,
        Revoked = t.Revoked,
    };

    private static RefreshToken Map(RefreshTokenEntity e) => new()
    {
        Token = e.Id,
        UserId = e.UserId,
        FamilyId = e.FamilyId,
        ExpiresAt = e.ExpiresAt,
        RedeemedAt = e.RedeemedAt,
        Revoked = e.Revoked,
    };
}

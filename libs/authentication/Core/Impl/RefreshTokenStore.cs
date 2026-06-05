using System.Collections.Concurrent;

namespace Sencilla.Authentication;

/// <summary>A single rotating refresh token belonging to a family (one login lineage).</summary>
public sealed class RefreshToken
{
    public required string Token { get; init; }
    public required Guid UserId { get; init; }
    public required Guid FamilyId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RedeemedAt { get; set; }
    public bool Revoked { get; set; }
}

/// <summary>Persistence seam for refresh tokens.</summary>
public interface IRefreshTokenStore
{
    Task Add(RefreshToken token, CancellationToken cancellation = default);
    Task<RefreshToken?> Find(string token, CancellationToken cancellation = default);
    Task Update(RefreshToken token, CancellationToken cancellation = default);
    Task RevokeFamily(Guid familyId, CancellationToken cancellation = default);
}

/// <summary>Default in-memory store for embedded single-instance use.</summary>
public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

    public Task Add(RefreshToken token, CancellationToken cancellation = default)
    {
        _tokens[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> Find(string token, CancellationToken cancellation = default)
        => Task.FromResult(_tokens.TryGetValue(token, out var t) ? t : null);

    public Task Update(RefreshToken token, CancellationToken cancellation = default)
    {
        _tokens[token.Token] = token;
        return Task.CompletedTask;
    }

    public Task RevokeFamily(Guid familyId, CancellationToken cancellation = default)
    {
        foreach (var token in _tokens.Values.Where(t => t.FamilyId == familyId))
            token.Revoked = true;
        return Task.CompletedTask;
    }
}

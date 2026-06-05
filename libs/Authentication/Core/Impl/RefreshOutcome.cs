namespace Sencilla.Authentication;

/// <summary>The result of presenting a refresh token for redemption.</summary>
public enum RefreshOutcome
{
    /// <summary>A fresh token was issued (no prior token presented).</summary>
    Issued,

    /// <summary>The presented token was valid; a new token replaces it.</summary>
    Rotated,

    /// <summary>The token was unknown, expired, or revoked.</summary>
    Invalid,

    /// <summary>An already-redeemed token was replayed — the whole family is revoked.</summary>
    ReuseDetected,
}

public sealed record RefreshResult(RefreshOutcome Outcome, string? Token, Guid? UserId);

/// <summary>
/// Rotating refresh tokens with family reuse-detection. Each login starts a family; every redeem
/// rotates the token within the family; replaying a previously-redeemed token is treated as theft
/// and burns the entire family. Pure logic over <see cref="IRefreshTokenStore"/>, so it is
/// exhaustively unit-tested.
/// </summary>
public sealed class RefreshTokenService(IRefreshTokenStore store, IOptions<RefreshTokenOptions> options)
{
    private readonly RefreshTokenOptions _options = options.Value;

    /// <summary>Start a new family (on login) or extend an existing one (on rotation).</summary>
    public async Task<string> Issue(Guid userId, Guid? familyId = null, CancellationToken cancellation = default)
    {
        var token = new RefreshToken
        {
            Token = GenerateToken(),
            UserId = userId,
            FamilyId = familyId ?? Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(_options.LifetimeDays),
        };
        await store.Add(token, cancellation);
        return token.Token;
    }

    public async Task<RefreshResult> Redeem(string rawToken, CancellationToken cancellation = default)
    {
        var existing = await store.Find(rawToken, cancellation);
        if (existing is null || existing.Revoked || existing.ExpiresAt < DateTime.UtcNow)
            return new RefreshResult(RefreshOutcome.Invalid, null, null);

        // Replay of a token that was already rotated away → theft. Revoke the lineage.
        if (existing.RedeemedAt is not null)
        {
            if (_options.DetectReuse)
                await store.RevokeFamily(existing.FamilyId, cancellation);
            return new RefreshResult(RefreshOutcome.ReuseDetected, null, existing.UserId);
        }

        existing.RedeemedAt = DateTime.UtcNow;
        await store.Update(existing, cancellation);

        if (!_options.Rotate)
            return new RefreshResult(RefreshOutcome.Rotated, existing.Token, existing.UserId);

        var next = await Issue(existing.UserId, existing.FamilyId, cancellation);
        return new RefreshResult(RefreshOutcome.Rotated, next, existing.UserId);
    }

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                  .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

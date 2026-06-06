namespace Sencilla.Authentication;

/// <summary>
/// Password reset via single-use hashed tokens (24-hour TTL). The raw token is never stored —
/// only its SHA-256 hash. Dispatches <see cref="PasswordResetRequested"/> for the host to handle
/// email delivery; always succeeds for unknown emails to prevent account enumeration.
/// </summary>
public sealed class AccountRecoveryService(
    IUserStore users,
    IPasswordHasher hasher,
    ICreateRepository<PasswordResetToken, Guid> create,
    IUpdateRepository<PasswordResetToken, Guid> update,
    IEventDispatcher events,
    IOptions<PasswordPolicyOptions> policy) : IAccountRecoveryService
{
    private readonly PasswordPolicyOptions _policy = policy.Value;

    public Task RequestEmailVerification(Guid userId, CancellationToken token = default)
        => Task.CompletedTask; // not in scope for this PRD

    public Task ConfirmEmail(string verificationToken, CancellationToken token = default)
        => Task.CompletedTask; // not in scope for this PRD

    public async Task RequestPasswordReset(string email, CancellationToken token = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await users.FindByEmail(normalized, token);
        if (user is null) return; // silent — prevents enumeration

        var rawToken = GenerateToken();
        var entity = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
        };
        await create.Create(entity, token);

        await events.PublishAsync(
            new PasswordResetRequested { UserId = user.Id, Email = normalized, Token = rawToken },
            token);
    }

    public async Task ResetPassword(string resetToken, string newPassword, CancellationToken token = default)
    {
        if (newPassword is null || newPassword.Length < _policy.MinLength)
            throw new BadRequestException("password-too-short");

        var hash = Hash(resetToken);
        var entity = await create.Query.FirstOrDefaultAsync(t => t.TokenHash == hash, token)
                  ?? throw new BadRequestException("token-invalid");

        if (entity.UsedAt is not null)
            throw new BadRequestException("token-already-used");
        if (entity.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("token-expired");

        entity.UsedAt = DateTime.UtcNow;
        await update.Update(entity, token);
        await users.SetPasswordHash(entity.UserId, hasher.Hash(newPassword), token);
        await events.PublishAsync(new PasswordChanged { UserId = entity.UserId }, token);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

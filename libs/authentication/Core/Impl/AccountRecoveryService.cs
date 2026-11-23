namespace Sencilla.Authentication;

/// <summary>
/// Emailed single-use tokens for password reset and address verification. The raw token is never
/// stored — only its SHA-256 hash, with a <see cref="TokenPurpose"/> so one kind can never be
/// presented as the other. Delivery is the host's job: <see cref="PasswordResetRequested"/> /
/// <see cref="EmailVerificationRequested"/> carry the raw token. Requesting a reset for an unknown
/// email succeeds silently (no enumeration). Repository access runs under
/// <see cref="Access.Root"/> because the caller is anonymous and the rows are framework-owned.
/// </summary>
public sealed class AccountRecoveryService(
    IUserStore users,
    IPasswordHasher hasher,
    ICreateRepository<PasswordResetToken, Guid> create,
    IUpdateRepository<PasswordResetToken, Guid> update,
    IEventDispatcher events,
    IServiceProvider services,
    IOptions<PasswordPolicyOptions> policy,
    IOptions<AppAccountsOptions> accounts) : IAccountRecoveryService
{
    private readonly PasswordPolicyOptions _policy = policy.Value;
    private readonly AppAccountsOptions _accounts = accounts.Value;

    public async Task RequestEmailVerification(Guid userId, string? email = null, CancellationToken token = default)
    {
        var user = await users.FindById(userId, token);
        if (user is null)
            return;

        var typed = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        if (typed is not null && !CredentialAuthService.IsPlainAddress(typed))
            throw new BadRequestException("email-invalid");
        if (string.Equals(typed, user.Email, StringComparison.OrdinalIgnoreCase))
            typed = null;

        if (typed is null)
        {
            if (!string.IsNullOrEmpty(user.Email))
            {
                if (!user.EmailConfirmed)
                    await SendVerificationLink(user.Id, user.Email, isPendingAddress: false, token);
            }
            else if (await FindPendingEmail(user.Id, token) is { } pending)
            {
                await SendVerificationLink(user.Id, pending, isPendingAddress: true, token);
            }
            return;
        }

        // Changing an existing address is not this flow: a password credential's login key is that address.
        if (!string.IsNullOrEmpty(user.Email))
            throw new BadRequestException("email-already-set");
        if (await users.FindByEmail(typed, token) is not null)
            throw new BadRequestException("email-taken");

        await SendVerificationLink(user.Id, typed, isPendingAddress: true, token);
    }

    public async Task<string?> FindPendingEmail(Guid userId, CancellationToken token = default)
    {
        using var _ = Access.Root();
        return await create.Query.AsNoTracking()
            .Where(t => t.UserId == userId && t.Purpose == TokenPurpose.Verify && t.UsedAt == null && t.Email != null)
            .OrderByDescending(t => t.ExpiresAt)
            .Select(t => t.Email)
            .FirstOrDefaultAsync(token);
    }

    public async Task<AuthUser> ConfirmEmail(string verificationToken, CancellationToken token = default)
    {
        var entity = await Consume(verificationToken, TokenPurpose.Verify, token);
        if (entity.Email is null)
            await users.SetEmailConfirmed(entity.UserId, token);
        else
            await users.SetConfirmedEmail(entity.UserId, entity.Email, token);
        return await users.FindById(entity.UserId, token) ?? throw new BadRequestException("token-invalid");
    }

    /// <summary>
    /// Mint a verify token for <paramref name="address"/> — carrying it when it is a pending address, not yet the
    /// account's — and hand the link to the host's mailer.
    /// </summary>
    private async Task SendVerificationLink(Guid userId, string address, bool isPendingAddress, CancellationToken token)
    {
        var rawToken = await IssueToken(userId, TokenPurpose.Verify, _accounts.VerificationTokenLifetime, token, isPendingAddress ? address : null);

        await events.PublishAsync(
            new EmailVerificationRequested { UserId = userId, Email = address, Token = rawToken },
            token);
    }

    public async Task RequestPasswordReset(string email, CancellationToken token = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await users.FindByEmail(normalized, token);
        if (user is null || user.Status is AuthUserStatus.Disabled or AuthUserStatus.Locked)
            return; // silent — prevents enumeration

        var rawToken = await IssueToken(user.Id, TokenPurpose.Reset, _accounts.ResetTokenLifetime, token);

        await events.PublishAsync(
            new PasswordResetRequested { UserId = user.Id, Email = normalized, Token = rawToken },
            token);
    }

    public async Task<AuthUser> ResetPassword(string resetToken, string newPassword, CancellationToken token = default)
    {
        PasswordPolicy.Validate(newPassword, _policy);

        var entity = await Consume(resetToken, TokenPurpose.Reset, token);

        var user = await users.FindById(entity.UserId, token) ?? throw new BadRequestException("token-invalid");
        if (user.Status is AuthUserStatus.Disabled or AuthUserStatus.Locked)
            throw new UnauthorizedException("account-not-active");

        await users.SetPasswordHash(entity.UserId, hasher.Hash(newPassword), token);

        // Following the emailed link proves control of the address.
        if (!user.EmailConfirmed)
            await users.SetEmailConfirmed(user.Id, token);

        // Whoever else holds a session for this account loses it; cookie sessions are cut by the
        // CredentialsChangedAt validator, refresh-token families here.
        if (services.GetService<IRefreshTokenStore>() is { } refresh)
            await refresh.RevokeUser(user.Id, token);

        await events.PublishAsync(new PasswordChanged { UserId = user.Id, Reason = "reset" }, token);

        return await users.FindById(user.Id, token) ?? user;
    }

    /// <summary>Retire every open token of this purpose for the user, then mint a fresh one.</summary>
    private async Task<string> IssueToken(Guid userId, string purpose, TimeSpan lifetime, CancellationToken token, string? email = null)
    {
        var rawToken = GenerateToken();
        var now = DateTime.UtcNow;

        using (Access.Root())
        {
            var open = await create.Query
                .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null)
                .ToListAsync(token);
            foreach (var stale in open)
            {
                stale.UsedAt = now;
                await update.Update(stale, token);
            }

            await create.Create(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = Hash(rawToken),
                ExpiresAt = now.Add(lifetime),
                Purpose = purpose,
                Email = email,
            }, token);
        }

        return rawToken;
    }

    /// <summary>Find the row for a raw token of the given purpose, validate it, and burn it.</summary>
    private async Task<PasswordResetToken> Consume(string rawToken, string purpose, CancellationToken token)
    {
        using var _ = Access.Root();

        var hash = Hash(rawToken ?? string.Empty);
        var entity = await create.Query.FirstOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == purpose, token)
                  ?? throw new BadRequestException("token-invalid");

        if (entity.UsedAt is not null)
            throw new BadRequestException("token-already-used");
        if (entity.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("token-expired");

        entity.UsedAt = DateTime.UtcNow;
        await update.Update(entity, token);
        return entity;
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

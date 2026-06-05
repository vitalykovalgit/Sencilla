namespace Sencilla.Authentication;

/// <summary>
/// Password registration / login / change-password over the user store. Stays transaction-free —
/// signup uses the store's coarse atomic operation. Verifies in constant time on a miss (dummy
/// hash) to avoid user enumeration, rehashes weak hashes on login, and raises domain events.
/// </summary>
public sealed class CredentialAuthService : ICredentialAuthService
{
    // Computed once; gives a non-existent account a comparable verification cost.
    private static readonly Lazy<string> DummyHash =
        new(() => new Argon2idPasswordHasher(Options.Create(new PasswordPolicyOptions())).Hash("timing-equalizer"));

    private readonly IUserStore _users;
    private readonly IPasswordHasher _hasher;
    private readonly IClaimsPrincipalFactory _principals;
    private readonly ITokenIssuer _issuer;
    private readonly IEventDispatcher _events;
    private readonly PasswordPolicyOptions _policy;
    private readonly AppAccountsOptions _accounts;

    public CredentialAuthService(
        IUserStore users,
        IPasswordHasher hasher,
        IClaimsPrincipalFactory principals,
        ITokenIssuer issuer,
        IEventDispatcher events,
        IOptions<PasswordPolicyOptions> policy,
        IOptions<AppAccountsOptions> accounts)
    {
        _users = users;
        _hasher = hasher;
        _principals = principals;
        _issuer = issuer;
        _events = events;
        _policy = policy.Value;
        _accounts = accounts.Value;
    }

    public async Task<TokenResponse> Register(RegisterRequest request, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("email-required");
        if (request.Password is null || request.Password.Length < _policy.MinLength)
            throw new BadRequestException("password-too-short");

        var email = Normalize(request.Email);
        if (await _users.FindByEmail(email, token) is not null)
            throw new BadRequestException("email-already-registered");

        var user = new AuthUser(Guid.NewGuid(), email, EmailConfirmed: false, AuthUserStatus.PendingVerification, request.Phone);
        var created = await _users.CreateWithPasswordCredential(user, _hasher.Hash(request.Password), token);

        await _events.PublishAsync(new UserRegistered { UserId = created.Id, Email = created.Email, Method = "password" }, token);

        return await IssueFor(created, token);
    }

    public async Task<TokenResponse> Login(LoginRequest request, CancellationToken token = default)
    {
        // Resolve the login identifier (email, or phone when AllowPhoneLogin is enabled) to an email.
        var email = await ResolveLoginEmail(request.Login, token);
        var credential = email is null ? null : await _users.FindPasswordCredential(email, token);

        // Always verify (against a dummy hash when the account is missing) to keep timing uniform.
        var hash = credential?.PasswordHash ?? DummyHash.Value;
        var valid = _hasher.Verify(request.Password ?? string.Empty, hash);

        if (credential is null || !valid)
        {
            await _events.PublishAsync(new LoginFailed { Email = email ?? request.Login, Reason = "invalid-credentials" }, token);
            throw new UnauthorizedException("invalid-credentials");
        }

        if (credential.User.Status is AuthUserStatus.Locked or AuthUserStatus.Disabled)
            throw new UnauthorizedException("account-not-active");

        if (_hasher.NeedsRehash(hash))
            await _users.SetPasswordHash(credential.User.Id, _hasher.Hash(request.Password!), token);

        await _events.PublishAsync(new LoginSucceeded { UserId = credential.User.Id, Method = "password" }, token);

        return await IssueFor(credential.User, token);
    }

    public async Task ChangePassword(Guid userId, string currentPassword, string newPassword, CancellationToken token = default)
    {
        if (newPassword is null || newPassword.Length < _policy.MinLength)
            throw new BadRequestException("password-too-short");

        var user = await _users.FindById(userId, token) ?? throw new UnauthorizedException("user-not-found");
        if (string.IsNullOrEmpty(user.Email))
            throw new BadRequestException("no-password-credential");

        var credential = await _users.FindPasswordCredential(Normalize(user.Email), token);
        if (credential is null || !_hasher.Verify(currentPassword, credential.PasswordHash))
            throw new UnauthorizedException("invalid-credentials");

        await _users.SetPasswordHash(userId, _hasher.Hash(newPassword), token);
        await _events.PublishAsync(new PasswordChanged { UserId = userId }, token);
    }

    private async Task<TokenResponse> IssueFor(AuthUser user, CancellationToken token)
    {
        var principal = await _principals.Create(user, scopes: null, token);
        return await _issuer.Issue(principal, token);
    }

    /// <summary>
    /// Map a login identifier to the account email. An identifier containing '@' is treated as an
    /// email; otherwise, when phone login is enabled, it is parsed as a phone number and resolved
    /// against the store. Returns null when nothing matches (the caller then fails uniformly).
    /// </summary>
    private async Task<string?> ResolveLoginEmail(string login, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(login))
            return null;
        if (login.Contains('@'))
            return Normalize(login);
        if (!_accounts.PhoneLoginEnabled || !TryParsePhone(login, out var phone))
            return null;
        var user = await _users.FindByPhone(phone, token);
        return string.IsNullOrEmpty(user?.Email) ? null : Normalize(user!.Email!);
    }

    private static bool TryParsePhone(string value, out long phone)
    {
        phone = 0;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length > 0 && long.TryParse(digits, out phone);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}

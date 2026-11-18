namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// End-to-end token-API path over the real EF bridge (InMemory): the Users bridge + Argon2id hasher
/// + claims factory + HMAC issuer wired exactly as a host would, exercising register / login /
/// external-exchange / recovery / verification and validating the access token issued for the user
/// the services return.
/// </summary>
public sealed class TokenApiFlowTests : IDisposable
{
    private const string SigningKey = "integration-test-signing-key-32-bytes!!";

    private readonly AuthTestDbContext _db;
    private readonly EfUserStore _store;
    private readonly CredentialAuthService _credentials;
    private readonly ExternalAuthService _external;
    private readonly AccountRecoveryService _recovery;
    private readonly ClaimsPrincipalFactory _principals;
    private readonly JwtTokenIssuer _issuer;
    private readonly RecordingEventDispatcher _events = new();
    private readonly AppAccountsOptions _accounts = new();
    private readonly InMemoryRefreshTokenStore _refreshStore = new();

    public TokenApiFlowTests()
    {
        var options = new DbContextOptionsBuilder<AuthTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AuthTestDbContext(options);

        var dependency = new RepositoryDependency(
            new ServiceCollection().BuildServiceProvider(),
            new NoopEventDispatcher(),
            new Mock<ICommandDispatcher>().Object);

        _store = new EfUserStore(
            new ReadRepository<User, AuthTestDbContext, Guid>(dependency, _db),
            new CreateRepository<User, AuthTestDbContext, Guid>(dependency, _db),
            new UpdateRepository<User, AuthTestDbContext, Guid>(dependency, _db),
            new ReadRepository<UserAuth, AuthTestDbContext, Guid>(dependency, _db),
            new CreateRepository<UserAuth, AuthTestDbContext, Guid>(dependency, _db),
            new UpdateRepository<UserAuth, AuthTestDbContext, Guid>(dependency, _db),
            new DeleteRepository<UserAuth, AuthTestDbContext, Guid>(dependency, _db),
            new ReadRepository<UserRole, AuthTestDbContext, Guid>(dependency, _db));

        var hasher = new Argon2idPasswordHasher(Options.Create(new PasswordPolicyOptions { Argon2MemoryKb = 8192, Argon2Iterations = 1 }));
        _principals = new ClaimsPrincipalFactory(_store, Array.Empty<IClaimsEnricher>());
        var refresh = new RefreshTokenService(_refreshStore, Options.Create(new RefreshTokenOptions()));
        _issuer = new JwtTokenIssuer(
            Options.Create(new JwtIssuerOptions { SigningKey = SigningKey, Issuer = "test", Audience = "test-api" }), refresh);
        var policy = Options.Create(new PasswordPolicyOptions { MinLength = 8, Argon2MemoryKb = 8192, Argon2Iterations = 1 });

        var services = new ServiceCollection().AddSingleton<IRefreshTokenStore>(_refreshStore).BuildServiceProvider();
        _recovery = new AccountRecoveryService(
            _store, hasher,
            new CreateRepository<PasswordResetToken, AuthTestDbContext, Guid>(dependency, _db),
            new UpdateRepository<PasswordResetToken, AuthTestDbContext, Guid>(dependency, _db),
            _events, services, policy, Options.Create(_accounts));

        _credentials = new CredentialAuthService(_store, hasher, _events, _recovery, policy, Options.Create(_accounts));
        _external = new ExternalAuthService(
            new IProviderTokenVerifier[] { new FakeGoogleVerifier() }, _store, new AccountLinkingPolicy(), _events);
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsTheUser_WhoseTokenValidates()
    {
        var registered = await _credentials.Register(new RegisterRequest { Email = "Alice@Example.com", Password = "Sup3rSecret!" });

        var login = await _credentials.Login(new LoginRequest { Login = "alice@example.com", Password = "Sup3rSecret!" });
        Assert.Equal(registered.Id, login.Id);
        Assert.Equal("alice@example.com", login.Email);              // email normalized on register

        var tokens = await SignIn(login);
        Assert.Equal(login.Id.ToString(), await AssertValidAccessToken(tokens.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        await _credentials.Register(new RegisterRequest { Email = "dup@example.com", Password = "Sup3rSecret!" });
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _credentials.Register(new RegisterRequest { Email = "dup@example.com", Password = "Sup3rSecret!" }));
    }

    [Theory]
    [InlineData("Support <victim@example.com>")]
    [InlineData("not-an-address")]
    [InlineData("a@b")]
    public async Task Register_RejectsAnythingButAPlainAddress(string email)
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            _credentials.Register(new RegisterRequest { Email = email, Password = "Sup3rSecret!" }));
        Assert.Equal("email-invalid", ex.Message);
    }

    [Fact]
    public async Task ExternalExchange_KeepsNameAndPicture()
    {
        await _external.Exchange("google", "fake-id-token");
        var row = _db.Users.AsNoTracking().Single(u => u.Email == "ext@example.com");
        Assert.Equal("Ext", row.FirstName);
        Assert.Equal("User", row.LastName);
    }

    [Fact]
    public async Task ExternalSignIn_KeepsACompoundGivenNameWhole_AndTheFamilyNameAsTheLastName()
    {
        await _external.ResolveAccount(new ExternalIdentity("google", "google-sub-anna", "anna@example.com", EmailVerified: true,
            Name: "Anna Maria Koval", GivenName: "Anna Maria", FamilyName: "Koval"));

        var row = _db.Users.AsNoTracking().Single(u => u.Email == "anna@example.com");
        Assert.Equal("Anna Maria", row.FirstName);
        Assert.Equal("Koval", row.LastName);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await _credentials.Register(new RegisterRequest { Email = "bob@example.com", Password = "Sup3rSecret!" });
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _credentials.Login(new LoginRequest { Login = "bob@example.com", Password = "wrong-password" }));
        Assert.Equal("invalid-credentials", ex.Message);              // uniform by default
    }

    [Fact]
    public async Task Login_UnknownUser_ThrowsUnauthorized()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _credentials.Login(new LoginRequest { Login = "ghost@example.com", Password = "whatever123" }));
        Assert.Equal("invalid-credentials", ex.Message);
    }

    [Fact]
    public async Task Login_DetailedErrors_SayWhy()
    {
        _accounts.DetailedLoginErrors = true;
        await _credentials.Register(new RegisterRequest { Email = "carol@example.com", Password = "Sup3rSecret!" });
        await _external.Exchange("google", "fake-id-token");           // ext@example.com, Google only

        Assert.Equal("account-not-found", (await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _credentials.Login(new LoginRequest { Login = "nobody@example.com", Password = "whatever123" }))).Message);
        Assert.Equal("invalid-password", (await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _credentials.Login(new LoginRequest { Login = "carol@example.com", Password = "wrong-password" }))).Message);
        Assert.Equal("password-not-set", (await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _credentials.Login(new LoginRequest { Login = "ext@example.com", Password = "whatever123" }))).Message);
    }

    [Fact]
    public async Task Register_PublishesVerificationToken_AndConfirmEmailFlipsTheFlag()
    {
        await _credentials.Register(new RegisterRequest { Email = "dana@example.com", Password = "Sup3rSecret!" });

        var requested = Assert.Single(_events.Of<EmailVerificationRequested>());
        Assert.False((await _store.FindByEmail("dana@example.com"))!.EmailConfirmed);

        var confirmed = await _recovery.ConfirmEmail(requested.Token);
        Assert.True(confirmed.EmailConfirmed);
        Assert.True((await _store.FindByEmail("dana@example.com"))!.EmailConfirmed);

        var again = await Assert.ThrowsAsync<BadRequestException>(() => _recovery.ConfirmEmail(requested.Token));
        Assert.Equal("token-already-used", again.Message);

        // A verification token is not a reset token.
        await _credentials.Register(new RegisterRequest { Email = "erin@example.com", Password = "Sup3rSecret!" });
        var erin = _events.Of<EmailVerificationRequested>().Last();
        Assert.Equal("token-invalid", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.ResetPassword(erin.Token, "N3wSecret!!"))).Message);
    }

    [Fact]
    public async Task ForgotThenReset_ChangesPassword_ConfirmsEmail_RevokesRefreshTokens_AndBurnsOlderLinks()
    {
        var registered = await SignIn(await _credentials.Register(new RegisterRequest { Email = "fay@example.com", Password = "Sup3rSecret!" }));
        var before = (await _store.FindByEmail("fay@example.com"))!;

        await _recovery.RequestPasswordReset("fay@example.com");
        await _recovery.RequestPasswordReset("Fay@Example.com");
        var tokens = _events.Of<PasswordResetRequested>().Select(e => e.Token).ToArray();
        Assert.Equal(2, tokens.Length);

        // The earlier link was retired by the later request.
        Assert.Equal("token-already-used", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.ResetPassword(tokens[0], "N3wSecret!!"))).Message);

        var user = await _recovery.ResetPassword(tokens[1], "N3wSecret!!");

        Assert.True(user.EmailConfirmed);
        Assert.NotNull(user.CredentialsChangedAt);
        Assert.Null(before.CredentialsChangedAt);
        await _credentials.Login(new LoginRequest { Login = "fay@example.com", Password = "N3wSecret!!" });
        await Assert.ThrowsAsync<UnauthorizedException>(() => _credentials.Login(new LoginRequest { Login = "fay@example.com", Password = "Sup3rSecret!" }));
        Assert.Contains(_events.Of<PasswordChanged>(), e => e.Reason == "reset");

        // The session issued at registration is gone.
        var found = await _refreshStore.Find(registered.RefreshToken!);
        Assert.True(found!.Revoked);
    }

    [Fact]
    public async Task ResetToken_HonoursConfiguredLifetime()
    {
        _accounts.ResetTokenLifetime = TimeSpan.FromMinutes(-1);       // already expired when issued
        await _credentials.Register(new RegisterRequest { Email = "gus@example.com", Password = "Sup3rSecret!" });
        await _recovery.RequestPasswordReset("gus@example.com");
        var token = _events.Of<PasswordResetRequested>().Single().Token;

        Assert.Equal("token-expired", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.ResetPassword(token, "N3wSecret!!"))).Message);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_IsSilent()
    {
        await _recovery.RequestPasswordReset("nobody@example.com");
        Assert.Empty(_events.Of<PasswordResetRequested>());
    }

    [Fact]
    public async Task ExternalExchange_CreatesUser_ThenLinksSameAccount()
    {
        var first = await _external.Exchange("google", "fake-id-token");

        var linked = await _store.FindByProviderKey("google", "google-sub-xyz");
        Assert.NotNull(linked);
        Assert.Equal(linked!.Id, first.Id);
        Assert.Equal("ext@example.com", linked.Email);

        // Signing in again with the same provider identity resolves to the same account.
        var second = await _external.Exchange("google", "fake-id-token");
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task LinkProvider_AdoptsALegacyRowInsteadOfDuplicating()
    {
        // What the first-login middleware wrote for years: scheme name, no subject.
        var user = _db.Users.Add(new User { Id = Guid.NewGuid(), Email = "legacy@example.com", EmailConf = true, Status = 2 }).Entity;
        _db.UserAuths.Add(new UserAuth { Id = Guid.NewGuid(), UserId = user.Id, Auth = "Google", Email = user.Email });
        await _db.SaveChangesAsync();
        NewRequest();

        await _store.LinkProvider(user.Id, new ExternalIdentity("google", "sub-legacy", "legacy@example.com", true));

        var rows = _db.UserAuths.Where(a => a.UserId == user.Id).ToList();
        Assert.Single(rows);
        Assert.Equal("sub-legacy", rows[0].ProviderKey);
        Assert.NotNull(await _store.FindByProviderKey("google", "sub-legacy"));
    }

    [Fact]
    public async Task RemovePasswordCredential_BumpsCredentialsChangedAt()
    {
        await _credentials.Register(new RegisterRequest { Email = "hal@example.com", Password = "Sup3rSecret!" });
        NewRequest();
        var user = (await _store.FindByEmail("hal@example.com"))!;
        Assert.True(await _store.HasPasswordCredential(user.Id));

        await _store.RemovePasswordCredential(user.Id);

        Assert.False(await _store.HasPasswordCredential(user.Id));
        Assert.NotNull((await _store.FindById(user.Id))!.CredentialsChangedAt);
    }

    [Fact]
    public async Task DbRefreshTokenStore_KeepsOnlyTheHashAtRest()
    {
        var dependency = new RepositoryDependency(new ServiceCollection().BuildServiceProvider(), new NoopEventDispatcher(), new Mock<ICommandDispatcher>().Object);
        var store = new DbRefreshTokenStore(
            new CreateRepository<RefreshTokenEntity, AuthTestDbContext, string>(dependency, _db),
            new UpdateRepository<RefreshTokenEntity, AuthTestDbContext, string>(dependency, _db));
        var userId = Guid.NewGuid();
        var token = new RefreshToken { Token = "raw-refresh-token", UserId = userId, FamilyId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(1) };

        await store.Add(token);
        NewRequest();

        Assert.Null(_db.RefreshTokens.Find("raw-refresh-token"));
        Assert.NotNull(_db.RefreshTokens.Find(DbRefreshTokenStore.Hash("raw-refresh-token")));
        NewRequest();

        var found = await store.Find("raw-refresh-token");
        Assert.Equal("raw-refresh-token", found!.Token);

        found.RedeemedAt = DateTime.UtcNow;
        await store.Update(found);
        Assert.NotNull((await store.Find("raw-refresh-token"))!.RedeemedAt);
        // RevokeUser / RevokeFamily are set-based (ExecuteUpdate), which the InMemory provider cannot run;
        // the in-memory store covers the semantics in RefreshTokenServiceTests.
    }

    /// <summary>
    /// The repositories share one DbContext here, as they do within one request. <c>Create</c> leaves
    /// its entity tracked, so a flow that a host runs across two requests (register, then something
    /// that updates the same row) needs the boundary made explicit.
    /// </summary>
    private void NewRequest() => _db.ChangeTracker.Clear();

    /// <summary>What a sign-in endpoint does with the returned user in token mode.</summary>
    private async Task<TokenResponse> SignIn(AuthUser user) => await _issuer.Issue(await _principals.Create(user, scopes: null));

    private static async Task<string> AssertValidAccessToken(string token)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "test",
            ValidateAudience = true,
            ValidAudience = "test-api",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        });

        Assert.True(result.IsValid, result.Exception?.Message);
        return ((JsonWebToken)result.SecurityToken).Subject;
    }

    public void Dispose() => _db.Dispose();

    private sealed class FakeGoogleVerifier : IProviderTokenVerifier
    {
        public string Provider => "google";

        public Task<ExternalIdentity> Verify(string idToken, CancellationToken token = default)
            => Task.FromResult(new ExternalIdentity("google", "google-sub-xyz", "ext@example.com", EmailVerified: true, "Ext User"));
    }

    private sealed class NoopEventDispatcher : IEventDispatcher
    {
        public Task PublishAsync<T>(T @event, CancellationToken token) where T : class, IEvent => Task.CompletedTask;
    }

    private sealed class RecordingEventDispatcher : IEventDispatcher
    {
        public List<IEvent> Events { get; } = new();

        public IEnumerable<T> Of<T>() where T : IEvent => Events.OfType<T>();

        public Task PublishAsync<T>(T @event, CancellationToken token) where T : class, IEvent
        {
            Events.Add(@event);
            return Task.CompletedTask;
        }
    }
}

public sealed class AuthTestDbContext : DbContext
{
    public AuthTestDbContext(DbContextOptions<AuthTestDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserAuth> UserAuths => Set<UserAuth>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
}

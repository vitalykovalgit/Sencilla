namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// End-to-end token-API path over the real EF bridge (InMemory): the Users bridge + Argon2id hasher
/// + claims factory + HMAC issuer wired exactly as a host would, exercising register / login /
/// external-exchange and validating the issued access token.
/// </summary>
public sealed class TokenApiFlowTests : IDisposable
{
    private const string SigningKey = "integration-test-signing-key-32-bytes!!";

    private readonly AuthTestDbContext _db;
    private readonly EfUserStore _store;
    private readonly CredentialAuthService _credentials;
    private readonly ExternalAuthService _external;

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
            new ReadRepository<UserRole, AuthTestDbContext, Guid>(dependency, _db));

        var hasher = new Argon2idPasswordHasher(Options.Create(new PasswordPolicyOptions { Argon2MemoryKb = 8192, Argon2Iterations = 1 }));
        var principals = new ClaimsPrincipalFactory(_store, Array.Empty<IClaimsEnricher>());
        var refresh = new RefreshTokenService(new InMemoryRefreshTokenStore(), Options.Create(new RefreshTokenOptions()));
        var issuer = new JwtTokenIssuer(
            Options.Create(new JwtIssuerOptions { SigningKey = SigningKey, Issuer = "test", Audience = "test-api" }), refresh);
        var events = new NoopEventDispatcher();
        var policy = Options.Create(new PasswordPolicyOptions { MinLength = 8, Argon2MemoryKb = 8192, Argon2Iterations = 1 });

        _credentials = new CredentialAuthService(_store, hasher, principals, issuer, events, policy, Options.Create(new AppAccountsOptions()));
        _external = new ExternalAuthService(
            new IProviderTokenVerifier[] { new FakeGoogleVerifier() }, _store, new AccountLinkingPolicy(), principals, issuer, events);
    }

    [Fact]
    public async Task Register_ThenLogin_IssuesValidatableToken()
    {
        await _credentials.Register(new RegisterRequest { Email = "Alice@Example.com", Password = "Sup3rSecret!" });

        var login = await _credentials.Login(new LoginRequest { Login ="alice@example.com", Password = "Sup3rSecret!" });
        var subject = await AssertValidAccessToken(login.AccessToken);

        var user = await _store.FindByEmail("alice@example.com");
        Assert.NotNull(user);
        Assert.Equal(user!.Id.ToString(), subject);                  // email normalized on register
        Assert.False(string.IsNullOrEmpty(login.RefreshToken));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        await _credentials.Register(new RegisterRequest { Email = "dup@example.com", Password = "Sup3rSecret!" });
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _credentials.Register(new RegisterRequest { Email = "dup@example.com", Password = "Sup3rSecret!" }));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await _credentials.Register(new RegisterRequest { Email = "bob@example.com", Password = "Sup3rSecret!" });
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _credentials.Login(new LoginRequest { Login ="bob@example.com", Password = "wrong-password" }));
    }

    [Fact]
    public async Task Login_UnknownUser_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _credentials.Login(new LoginRequest { Login ="ghost@example.com", Password = "whatever123" }));
    }

    [Fact]
    public async Task ExternalExchange_CreatesUser_ThenLinksSameAccount()
    {
        var first = await _external.Exchange("google", "fake-id-token");
        var subject = await AssertValidAccessToken(first.AccessToken);

        var linked = await _store.FindByProviderKey("google", "google-sub-xyz");
        Assert.NotNull(linked);
        Assert.Equal(linked!.Id.ToString(), subject);
        Assert.Equal("ext@example.com", linked.Email);

        // Signing in again with the same provider identity resolves to the same account.
        var second = await _external.Exchange("google", "fake-id-token");
        Assert.Equal(subject, await AssertValidAccessToken(second.AccessToken));
    }

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
}

public sealed class AuthTestDbContext : DbContext
{
    public AuthTestDbContext(DbContextOptions<AuthTestDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserAuth> UserAuths => Set<UserAuth>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
}

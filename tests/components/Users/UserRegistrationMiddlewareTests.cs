namespace Sencilla.Component.Users.Tests;

/// <summary>
/// Tests for <see cref="UserRegistrationMiddleware"/> caching optimization.
///
/// Covers: fast-path cache hit (no DI/repo), cache miss with DB lookup,
///         cache miss with user creation, anonymous user bypass, next delegate invocation.
/// </summary>
public class UserRegistrationMiddlewareTests
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly Mock<ICurrentUserProvider> _userProvider = new();
    private readonly Mock<ISystemVariable> _sysVars = new();
    private readonly Mock<ICreateRepository<User, Guid>> _userRepo = new();
    private readonly Mock<ICreateRepository<UserAuth, Guid>> _userAuthRepo = new();

    private bool _nextCalled;

    private UserRegistrationMiddleware CreateMiddleware()
    {
        _nextCalled = false;
        return new UserRegistrationMiddleware(
            _ => { _nextCalled = true; return Task.CompletedTask; },
            _cache);
    }

    private DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_userProvider.Object);
        services.AddSingleton(_sysVars.Object);
        services.AddSingleton<ICreateRepository<User, Guid>>(_userRepo.Object);
        services.AddSingleton<ICreateRepository<UserAuth, Guid>>(_userAuthRepo.Object);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        return context;
    }

    // ── Anonymous user ───────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_AnonymousUser_SkipsLookup_CallsNext()
    {
        var anonymousUser = new User(); // no email, phone=0 → IsAnonymous() = true
        _userProvider.Setup(p => p.CurrentUser).Returns(anonymousUser);

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        Assert.True(_nextCalled);
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Cache hit (fast path) ────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_CacheHit_SkipsDIResolution_UseCachedUser()
    {
        var userId = Guid.NewGuid();
        var cachedUser = new User { Id = userId, Email = "cached@test.com" };
        _cache.Set("user_by_email_cached@test.com", cachedUser);

        _userProvider.Setup(p => p.CurrentUser).Returns(new User { Email = "cached@test.com" });

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        Assert.True(_nextCalled);
        // Fast path: repo should NOT be called
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()), Times.Never);
        // System variable should be set with the cached user
        _sysVars.Verify(s => s.Set("user", It.Is<User>(u => u.Id == userId)), Times.Once);
    }

    // ── Cache miss, user exists in DB ────────────────────────────────────────

    [Fact]
    public async Task Invoke_CacheMiss_UserExistsInDb_CachesAndSetsUser()
    {
        var userId = Guid.NewGuid();
        var incomingUser = new User { Email = "db@test.com" };
        var dbUser = new User { Id = userId, Email = "db@test.com" };

        _userProvider.Setup(p => p.CurrentUser).Returns(incomingUser);
        _userRepo.Setup(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbUser);

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        Assert.True(_nextCalled);
        // Repo was called for lookup
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()), Times.Once);
        // Nothing is created since the user exists
        _userRepo.Verify(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()), Times.Never);
        // User should be cached after lookup
        Assert.True(_cache.TryGetValue("user_by_email_db@test.com", out User? cached));
        Assert.Equal(userId, cached!.Id);
    }

    // ── Cache miss, user not in DB (create) ──────────────────────────────────

    [Fact]
    public async Task Invoke_CacheMiss_UserNotInDb_CreatesAndCachesUser()
    {
        var userId = Guid.NewGuid();
        var incomingUser = new User { Email = "new@test.com" };
        var createdUser = new User { Id = userId, Email = "new@test.com" };

        _userProvider.Setup(p => p.CurrentUser).Returns(incomingUser);

        // First call (from Invoke): null — user doesn't exist
        // Second call (the re-check once the registration gate is held): still null
        // Third call (read back after GetOrCreateUserAsync): the created user
        _userRepo.SetupSequence(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null)
            .ReturnsAsync((User?)null)
            .ReturnsAsync(createdUser);

        _userRepo.Setup(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new GetOrCreateResult<User> { Created = [createdUser] });
        _userAuthRepo.Setup(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<UserAuth>>(), It.IsAny<System.Linq.Expressions.Expression<Func<UserAuth, object>>[]>()))
            .ReturnsAsync(new GetOrCreateResult<UserAuth> { Created = [new UserAuth { Auth = "", Email = "" }] });

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        Assert.True(_nextCalled);
        // The user and its sign-in were created — insert-only, never upserted
        _userRepo.Verify(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()), Times.Once);
        _userAuthRepo.Verify(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<UserAuth>>(), It.IsAny<System.Linq.Expressions.Expression<Func<UserAuth, object>>[]>()), Times.Once);
        // User should be cached
        Assert.True(_cache.TryGetValue("user_by_email_new@test.com", out User? cached));
        Assert.Equal(userId, cached!.Id);
    }

    // ── Parallel first requests (a new account's first page) ─────────────────

    /// <summary>
    /// The page a new user lands on fires its API calls at once, and every one misses the cache and
    /// finds no user. Before the registration gate each of them created the account, and their MERGEs
    /// deadlocked each other in SQL Server. One of them creates it now; the rest read it back.
    /// </summary>
    [Fact]
    public async Task Invoke_ParallelFirstRequests_CreateTheUserOnce()
    {
        var createdUser = new User { Id = Guid.NewGuid(), Email = "burst@test.com" };
        var created = false;

        _userProvider.Setup(p => p.CurrentUser).Returns(() => new User { Email = "burst@test.com" });
        _userRepo.Setup(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()))
            // Like a real query: the answer is what the table held when the lookup STARTED, not when it returned.
            .Returns(async () => { var seen = created ? createdUser : null; await Task.Delay(10); return seen; });
        _userRepo.Setup(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .Callback(() => created = true)
            .ReturnsAsync(new GetOrCreateResult<User> { Created = [createdUser] });
        _userAuthRepo.Setup(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<UserAuth>>(), It.IsAny<System.Linq.Expressions.Expression<Func<UserAuth, object>>[]>()))
            .ReturnsAsync(new GetOrCreateResult<UserAuth> { Created = [new UserAuth { Auth = "", Email = "" }] });

        var middleware = CreateMiddleware();
        await Task.WhenAll(Enumerable.Range(0, 15).Select(_ => middleware.Invoke(CreateHttpContext())));

        _userRepo.Verify(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()), Times.Once);
        _userAuthRepo.Verify(r => r.GetOrCreateAsync(It.IsAny<IEnumerable<UserAuth>>(), It.IsAny<System.Linq.Expressions.Expression<Func<UserAuth, object>>[]>()), Times.Once);
    }

    // ── No user provider ─────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_NoUserProvider_ReturnsEarly_DoesNotCallNext()
    {
        var services = new ServiceCollection();
        // Don't register ICurrentUserProvider
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var middleware = CreateMiddleware();
        await middleware.Invoke(context);

        Assert.False(_nextCalled);
    }

    // ── Second request uses cache (integration) ──────────────────────────────

    [Fact]
    public async Task Invoke_SecondRequest_UsesCacheFromFirstRequest()
    {
        var user = new User { Email = "repeat@test.com" };
        var dbUser = new User { Id = Guid.NewGuid(), Email = "repeat@test.com" };

        _userProvider.Setup(p => p.CurrentUser).Returns(user);
        _userRepo.Setup(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbUser);

        var middleware = CreateMiddleware();

        // First request — cache miss, hits DB
        await middleware.Invoke(CreateHttpContext());
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()), Times.Once);

        // Second request — cache hit, skips DB
        await middleware.Invoke(CreateHttpContext());
        // Still only 1 call total — second request used cache
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

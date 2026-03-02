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
    private readonly Mock<ICreateRepository<User>> _userRepo = new();
    private readonly Mock<ICreateRepository<UserAuth, byte>> _userAuthRepo = new();

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
        services.AddSingleton<ICreateRepository<User>>(_userRepo.Object);
        services.AddSingleton<ICreateRepository<UserAuth, byte>>(_userAuthRepo.Object);

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
        var cachedUser = new User { Id = 42, Email = "cached@test.com" };
        _cache.Set("user_by_email_cached@test.com", cachedUser);

        _userProvider.Setup(p => p.CurrentUser).Returns(new User { Email = "cached@test.com" });

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        Assert.True(_nextCalled);
        // Fast path: repo should NOT be called
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()), Times.Never);
        // System variable should be set with the cached user
        _sysVars.Verify(s => s.Set("user", It.Is<User>(u => u.Id == 42)), Times.Once);
    }

    // ── Cache miss, user exists in DB ────────────────────────────────────────

    [Fact]
    public async Task Invoke_CacheMiss_UserExistsInDb_CachesAndSetsUser()
    {
        var incomingUser = new User { Email = "db@test.com" };
        var dbUser = new User { Id = 10, Email = "db@test.com" };

        _userProvider.Setup(p => p.CurrentUser).Returns(incomingUser);
        _userRepo.Setup(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbUser);

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        Assert.True(_nextCalled);
        // Repo was called for lookup
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()), Times.Once);
        // UpsertAsync should NOT be called since user exists
        _userRepo.Verify(r => r.UpsertAsync(It.IsAny<User>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object?>>>(), null, null, It.IsAny<CancellationToken>()), Times.Never);
        // User should be cached after lookup
        Assert.True(_cache.TryGetValue("user_by_email_db@test.com", out User? cached));
        Assert.Equal(10, cached!.Id);
    }

    // ── Cache miss, user not in DB (create) ──────────────────────────────────

    [Fact]
    public async Task Invoke_CacheMiss_UserNotInDb_CreatesAndCachesUser()
    {
        var incomingUser = new User { Email = "new@test.com" };
        var createdUser = new User { Id = 99, Email = "new@test.com" };

        _userProvider.Setup(p => p.CurrentUser).Returns(incomingUser);

        // First call (from Invoke): returns null — user doesn't exist
        // Second call (from UpsertUserAsync): returns created user
        _userRepo.SetupSequence(r => r.FirstOrDefault(It.IsAny<UserFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(createdUser);

        _userRepo.Setup(r => r.UpsertAsync(It.IsAny<User>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object?>>>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incomingUser);
        _userAuthRepo.Setup(r => r.UpsertAsync(It.IsAny<UserAuth>(), It.IsAny<System.Linq.Expressions.Expression<Func<UserAuth, object?>>>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserAuth { Auth = "", Email = "" });

        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.Invoke(context);

        Assert.True(_nextCalled);
        // UpsertAsync was called to create user
        _userRepo.Verify(r => r.UpsertAsync(It.IsAny<User>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object?>>>(), null, null, It.IsAny<CancellationToken>()), Times.Once);
        // User should be cached
        Assert.True(_cache.TryGetValue("user_by_email_new@test.com", out User? cached));
        Assert.Equal(99, cached!.Id);
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
        var dbUser = new User { Id = 7, Email = "repeat@test.com" };

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

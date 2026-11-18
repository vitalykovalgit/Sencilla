namespace Sencilla.Component.Users.Tests;

/// <summary>
/// <see cref="CurrentUserMiddleware"/>: the session's account id → that user (cached), anything else → the
/// anonymous user; the next middleware always runs.
/// </summary>
public class CurrentUserMiddlewareTests
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly Mock<ISystemVariable> _sysVars = new();
    private readonly Mock<IReadRepository<User, Guid>> _userRepo = new();

    private bool _nextCalled;

    private CurrentUserMiddleware CreateMiddleware()
    {
        _nextCalled = false;
        return new CurrentUserMiddleware(_ => { _nextCalled = true; return Task.CompletedTask; }, _cache);
    }

    private DefaultHttpContext CreateHttpContext(params Claim[] claims)
    {
        var services = new ServiceCollection()
            .AddSingleton(_sysVars.Object)
            .AddSingleton(_userRepo.Object);

        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Length > 0 ? "test" : null)),
        };
    }

    private void VerifyCurrentUser(Func<User, bool> isExpected)
        => _sysVars.Verify(s => s.Set("user", It.Is<User>(u => isExpected(u))), Times.Once);

    private void VerifyCurrentUserIsAnonymous()
        => VerifyCurrentUser(u => u.Id == Guid.Empty && u.IsAnonymous() && u.Roles!.Single().RoleId == (int)RoleType.Anonymous);

    [Fact]
    public async Task Invoke_IdOfACachedUser_MakesItCurrent_WithoutReading()
    {
        var cached = new User { Id = Guid.NewGuid(), Email = "cached@test.com" };
        _cache.Set($"user_by_id_{cached.Id}", cached);

        await CreateMiddleware().Invoke(CreateHttpContext(new Claim(ClaimType.UserId, cached.Id.ToString())));

        Assert.True(_nextCalled);
        VerifyCurrentUser(u => ReferenceEquals(u, cached));
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<IFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_IdNotCached_LoadsTheUserById_AndCachesIt()
    {
        var stored = new User { Id = Guid.NewGuid(), Email = "db@test.com" };
        _userRepo.Setup(r => r.FirstOrDefault(It.IsAny<IFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        var middleware = CreateMiddleware();

        await middleware.Invoke(CreateHttpContext(new Claim(ClaimType.UserId, stored.Id.ToString())));
        await middleware.Invoke(CreateHttpContext(new Claim(ClaimType.UserId, stored.Id.ToString())));

        Assert.True(_nextCalled);
        _sysVars.Verify(s => s.Set("user", stored), Times.Exactly(2));
        // The second request was served from the cache.
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<IFilter>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invoke_UnknownId_RunsAsAnonymous()
    {
        _userRepo.Setup(r => r.FirstOrDefault(It.IsAny<IFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await CreateMiddleware().Invoke(CreateHttpContext(new Claim(ClaimType.UserId, Guid.NewGuid().ToString())));

        Assert.True(_nextCalled);
        VerifyCurrentUserIsAnonymous();
    }

    [Fact]
    public async Task Invoke_NoId_RunsAsAnonymous_WithoutReading()
    {
        // An email alone is not an account: nothing is looked up or created by it.
        await CreateMiddleware().Invoke(CreateHttpContext(new Claim(ClaimTypes.Email, "someone@test.com")));

        Assert.True(_nextCalled);
        VerifyCurrentUserIsAnonymous();
        _userRepo.Verify(r => r.FirstOrDefault(It.IsAny<IFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetUserId_ReadsSubThenNameIdentifier_WhenAGuid()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimType.UserId, id.ToString())])).GetUserId());
        Assert.Equal(id, new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())])).GetUserId());
        // A provider's own subject (Google's numbers) is not an account id.
        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1234567890")])).GetUserId());
        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity()).GetUserId());
    }
}

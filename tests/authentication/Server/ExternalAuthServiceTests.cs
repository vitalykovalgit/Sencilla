namespace Sencilla.Authentication.Server.Tests;

/// <summary>
/// The collision table behind <see cref="IExternalAuthService.ResolveAccount"/>, row by row, over a
/// mocked store and the real policy. "Google wins over an unverified password credential" is the
/// account-takeover defence for email-keyed identity.
/// </summary>
public class ExternalAuthServiceTests
{
    private static readonly ExternalIdentity Google = new("google", "sub-1", "Ada@Example.com", EmailVerified: true, "Ada");

    private readonly Mock<IUserStore> _store = new(MockBehavior.Strict);
    private readonly List<IEvent> _events = new();
    private readonly ExternalAuthService _service;

    public ExternalAuthServiceTests()
    {
        var events = new Mock<IEventDispatcher>();
        events.Setup(e => e.PublishAsync(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()))
              .Callback<IEvent, CancellationToken>((e, _) => _events.Add(e))
              .Returns(Task.CompletedTask);
        _service = new ExternalAuthService(Array.Empty<IProviderTokenVerifier>(), _store.Object, new AccountLinkingPolicy(), events.Object);
    }

    [Fact]
    public async Task KnownSubject_ReturnsThatAccount()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", true, AuthUserStatus.Active);
        _store.Setup(s => s.FindByProviderKey("google", "sub-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.ResolveAccount(Google);

        Assert.Equal(user.Id, result.Id);
        Assert.IsType<LoginSucceeded>(Assert.Single(_events));
    }

    [Fact]
    public async Task UnknownEmail_CreatesAccount()
    {
        _store.Setup(s => s.FindByProviderKey("google", "sub-1", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), Google, It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity _, CancellationToken _) => u);

        var result = await _service.ResolveAccount(Google);

        Assert.Equal("ada@example.com", result.Email);
        Assert.True(result.EmailConfirmed);
        Assert.Contains(_events, e => e is UserRegistered r && r.Method == "google");
    }

    [Fact]
    public async Task VerifiedPasswordAccount_IsLinkedSilently()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _store.Setup(s => s.FindByProviderKey("google", "sub-1", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _store.Setup(s => s.LinkProvider(user.Id, Google, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _store.Setup(s => s.FindById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.ResolveAccount(Google);

        Assert.Equal(user.Id, result.Id);
        _store.Verify(s => s.RemovePasswordCredential(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains(_events, e => e is ProviderLinked);
    }

    [Fact]
    public async Task UnverifiedPasswordAccount_LosesItsPasswordToTheVerifiedProvider()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: false, AuthUserStatus.Active);
        _store.Setup(s => s.FindByProviderKey("google", "sub-1", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _store.Setup(s => s.HasPasswordCredential(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _store.Setup(s => s.RemovePasswordCredential(user.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _store.Setup(s => s.LinkProvider(user.Id, Google, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _store.Setup(s => s.SetEmailConfirmed(user.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _store.Setup(s => s.FindById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user with { EmailConfirmed = true });

        var result = await _service.ResolveAccount(Google);

        Assert.True(result.EmailConfirmed);
        _store.Verify(s => s.RemovePasswordCredential(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(_events, e => e is PasswordChanged p && p.Reason == "removed");
    }

    [Fact]
    public async Task UnverifiedProviderEmail_OnAnExistingAccount_IsRefused()
    {
        // Whoever can put victim@ on a social profile must not get victim's account.
        var unverified = Google with { EmailVerified = false };
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _store.Setup(s => s.FindByProviderKey("google", "sub-1", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _service.ResolveAccount(unverified));

        Assert.Equal("email-not-verified", ex.Message);
        _store.Verify(s => s.LinkProvider(It.IsAny<Guid>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnverifiedProviderEmail_OnAFreshAddress_StillProvisions()
    {
        var unverified = Google with { EmailVerified = false };
        _store.Setup(s => s.FindByProviderKey("google", "sub-1", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), unverified, It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity _, CancellationToken _) => u);

        var result = await _service.ResolveAccount(unverified);

        Assert.False(result.EmailConfirmed);
    }

    [Fact]
    public async Task DisabledAccount_IsRejected()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", true, AuthUserStatus.Disabled);
        _store.Setup(s => s.FindByProviderKey("google", "sub-1", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUser?)null);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _service.ResolveAccount(Google));
        Assert.Equal("account-not-active", ex.Message);
    }
}

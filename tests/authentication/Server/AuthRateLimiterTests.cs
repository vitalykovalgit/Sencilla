using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sencilla.Authentication.Server.Tests;

public sealed class AuthRateLimiterTests : IDisposable
{
    private readonly AuthApiOptions _api = new();
    private readonly AuthRateLimiter _limiter;

    public AuthRateLimiterTests() => _limiter = new AuthRateLimiter(Options.Create(_api));

    public void Dispose() => _limiter.Dispose();

    private static HttpContext From(string address)
        => new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Parse(address) } };

    [Fact]
    public void AWindow_RefusesOnceItsPermitsAreSpent()
    {
        _api.RateLimits.RegisterPerAddress = new(2, TimeSpan.FromHours(1));
        var http = From("203.0.113.1");

        Assert.True(_limiter.AllowsRegistration(http));
        Assert.True(_limiter.AllowsRegistration(http));
        Assert.False(_limiter.AllowsRegistration(http));
    }

    [Fact]
    public void DifferentCallers_CountSeparately()
    {
        _api.RateLimits.RegisterPerAddress = new(1, TimeSpan.FromHours(1));
        _api.RateLimits.LoginPerAddressAndEmail = new(1, TimeSpan.FromHours(1));
        var http = From("203.0.113.1");

        Assert.True(_limiter.AllowsRegistration(http));
        Assert.False(_limiter.AllowsRegistration(http));
        Assert.True(_limiter.AllowsRegistration(From("203.0.113.2")));

        Assert.True(_limiter.AllowsLogin(http, "alice@example.com"));
        Assert.True(_limiter.AllowsLogin(http, "bob@example.com"));
    }

    [Fact]
    public void TheLogin_IsCountedTrimmedAndLowerCased()
    {
        _api.RateLimits.LoginPerAddressAndEmail = new(1, TimeSpan.FromHours(1));
        var http = From("203.0.113.1");

        Assert.True(_limiter.AllowsLogin(http, "alice@example.com"));
        Assert.False(_limiter.AllowsLogin(http, "  Alice@Example.COM "));
    }

    [Fact]
    public void TwoWindowsWithEqualNumbers_DoNotShareABucket()
    {
        _api.RateLimits.TokenPerAddress = new(1, TimeSpan.FromHours(1));
        _api.RateLimits.MailPerAddress = new(1, TimeSpan.FromHours(1));
        var http = From("203.0.113.1");

        Assert.True(_limiter.AllowsTokenUse(http));
        Assert.True(_limiter.AllowsMail(http, "alice@example.com"));
        Assert.False(_limiter.AllowsTokenUse(http));
    }

    [Fact]
    public void Disabled_AllowsEveryCall()
    {
        _api.RateLimits.Enabled = false;
        _api.RateLimits.RegisterPerAddress = new(1, TimeSpan.FromHours(1));
        var http = From("203.0.113.1");

        for (var call = 0; call < 5; call++)
            Assert.True(_limiter.AllowsRegistration(http));
    }

    [Fact]
    public async Task TheEleventhLogin_ForOneAddressAndLogin_Answers429()
    {
        var credentials = new Mock<ICredentialAuthService>();
        credentials.Setup(c => c.Login(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new UnauthorizedException("invalid-credentials"));
        var endpoint = new AppAccountsEndpoint(Options.Create(_api), _limiter, Options.Create(new PasswordPolicyOptions()));
        var request = new LoginRequest { Login = "alice@example.com", Password = "wrong-password" };

        for (var attempt = 1; attempt <= 10; attempt++)
            Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(await endpoint.Login(From("203.0.113.1"), request, credentials.Object, default)));

        var refused = await endpoint.Login(From("203.0.113.1"), request, credentials.Object, default);

        Assert.Equal(StatusCodes.Status429TooManyRequests, StatusOf(refused));
        Assert.Equal("""{"error":"rate-limited"}""", JsonSerializer.Serialize(((IValueHttpResult)refused).Value));
        credentials.Verify(c => c.Login(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
    }

    private static int? StatusOf(IResult result) => ((IStatusCodeHttpResult)result).StatusCode;
}

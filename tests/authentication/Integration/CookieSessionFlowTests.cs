using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Sencilla.Web.MinimalApi;

namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// The cookie session mode end to end through ASP.NET's real cookie handler on a <c>TestServer</c>: the
/// built-in endpoints sign in, and the next request carries the cookie back past the session validator.
/// </summary>
public sealed class CookieSessionFlowTests
{
    private readonly Mock<IUserStore> _store = new();
    private readonly Mock<IAccountRecoveryService> _recovery = new();

    [Fact]
    public async Task ResetPassword_SignsIn_AndTheNextRequestWithItsCookieIsStillSignedIn()
    {
        // As the real store does: the reset stamps the credential change a moment before the session is issued.
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _recovery.Setup(r => r.ResetPassword("reset-token", "N3wSecret!!", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(() => user = user with { CredentialsChangedAt = DateTime.UtcNow });
        _store.Setup(s => s.FindById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => user);
        // Every request re-checks the session, so the reset's own credential stamp is compared with its start.
        await using var app = await StartHost(o => o.ValidationInterval = TimeSpan.Zero);
        var client = app.GetTestClient();

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "reset-token", newPassword = "N3wSecret!!" });
        reset.EnsureSuccessStatusCode();

        Assert.Equal(user.Id.ToString(), await (await client.SendAsync(WhoAmIWithTheCookieOf(reset))).Content.ReadAsStringAsync());
        _store.Verify(s => s.FindById(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FirstRequestAfterASignIn_AnswersWithTheUser_WithoutReadingTheStoreOrReissuingTheCookie()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _recovery.Setup(r => r.ResetPassword("reset-token", "N3wSecret!!", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        await using var app = await StartHost();
        var client = app.GetTestClient();

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "reset-token", newPassword = "N3wSecret!!" });
        reset.EnsureSuccessStatusCode();

        var whoami = await client.SendAsync(WhoAmIWithTheCookieOf(reset));
        Assert.Equal(user.Id.ToString(), await whoami.Content.ReadAsStringAsync());
        Assert.False(whoami.Headers.Contains("Set-Cookie"));
        _store.Verify(s => s.FindById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendVerification_PassesATypedAddressOn_AndAnswersARefusalWith400AndItsCode()
    {
        var user = new AuthUser(Guid.NewGuid(), Email: null, EmailConfirmed: false, AuthUserStatus.Active);
        _recovery.Setup(r => r.RequestEmailVerification(user.Id, "taken@example.com", It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new BadRequestException("email-taken"));
        await using var app = await StartHost();
        var client = app.GetTestClient();
        var signIn = await SignIn(client, user);

        var typed = await client.SendAsync(WithTheCookieOf(signIn, PostJson("/api/v1/auth/resend-verification", new { email = "new@example.com" })));
        var none = await client.SendAsync(WithTheCookieOf(signIn, PostJson("/api/v1/auth/resend-verification", new { })));
        var taken = await client.SendAsync(WithTheCookieOf(signIn, PostJson("/api/v1/auth/resend-verification", new { email = "taken@example.com" })));

        Assert.Equal(HttpStatusCode.OK, typed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, none.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
        Assert.Equal("""{"error":"email-taken"}""", await taken.Content.ReadAsStringAsync());
        _recovery.Verify(r => r.RequestEmailVerification(user.Id, "new@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _recovery.Verify(r => r.RequestEmailVerification(user.Id, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PendingEmail_AnswersTheSignedInAccountsPendingAddress_OrNull_AndNothingWithoutASession()
    {
        var user = new AuthUser(Guid.NewGuid(), Email: null, EmailConfirmed: false, AuthUserStatus.Active);
        _recovery.SetupSequence(r => r.FindPendingEmail(user.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync("new@example.com")
                 .ReturnsAsync((string?)null);
        await using var app = await StartHost();
        var client = app.GetTestClient();
        var signIn = await SignIn(client, user);

        var pending = await client.SendAsync(WithTheCookieOf(signIn, new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/pending-email")));
        var none = await client.SendAsync(WithTheCookieOf(signIn, new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/pending-email")));
        var anonymous = await client.GetAsync("/api/v1/auth/pending-email");

        Assert.Equal("""{"email":"new@example.com"}""", await pending.Content.ReadAsStringAsync());
        Assert.Equal("""{"email":null}""", await none.Content.ReadAsStringAsync());
        // Without a session the cookie scheme's challenge answers (a host maps it to 401 for /api).
        Assert.NotEqual(HttpStatusCode.OK, anonymous.StatusCode);
        _recovery.Verify(r => r.FindPendingEmail(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>A cookie session for <paramref name="user"/>, started by the reset endpoint over the faked recovery service.</summary>
    private async Task<HttpResponseMessage> SignIn(HttpClient client, AuthUser user)
    {
        _recovery.Setup(r => r.ResetPassword("reset-token", "N3wSecret!!", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "reset-token", newPassword = "N3wSecret!!" });
        reset.EnsureSuccessStatusCode();
        return reset;
    }

    private static HttpRequestMessage PostJson(string path, object body) => new(HttpMethod.Post, path) { Content = JsonContent.Create(body) };

    private static HttpRequestMessage WhoAmIWithTheCookieOf(HttpResponseMessage signIn)
        => WithTheCookieOf(signIn, new HttpRequestMessage(HttpMethod.Get, "/whoami"));

    private static HttpRequestMessage WithTheCookieOf(HttpResponseMessage signIn, HttpRequestMessage request)
    {
        request.Headers.Add("Cookie", string.Join("; ", signIn.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0])));
        return request;
    }

    private async Task<WebApplication> StartHost(Action<CookieSessionOptions>? cookieSession = null)
    {
        _store.Setup(s => s.GetRoles(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        builder.Services.AddAuthorization();
        builder.Services.AddSencillaAuthentication(builder.Configuration, a => a
            .UseCookieSession(cookieSession)
            .UseAppAccounts()
            .UseUsers(u => u.EntityFrameworkStore()));
        // Registered after the family, so the fakes win over its store and recovery service.
        builder.Services.AddSingleton(_store.Object).AddSingleton(_recovery.Object).AddSingleton(new Mock<IEventDispatcher>().Object);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSencillaEndpoints();
        app.MapGet("/whoami", (HttpContext http) => http.User.FindFirst(AuthClaims.Subject)?.Value ?? "anonymous");
        await app.StartAsync();
        return app;
    }
}

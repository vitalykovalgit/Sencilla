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

    private static HttpRequestMessage WhoAmIWithTheCookieOf(HttpResponseMessage signIn)
    {
        var whoami = new HttpRequestMessage(HttpMethod.Get, "/whoami");
        whoami.Headers.Add("Cookie", string.Join("; ", signIn.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0])));
        return whoami;
    }

    private async Task<WebApplication> StartHost(Action<CookieSessionOptions>? cookieSession = null)
    {
        _store.Setup(s => s.GetRoles(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        builder.Services.AddSencillaAuthentication(builder.Configuration, a => a
            .UseCookieSession(cookieSession)
            .UseAppAccounts()
            .UseUsers(u => u.EntityFrameworkStore()));
        // Registered after the family, so the fakes win over its store and recovery service.
        builder.Services.AddSingleton(_store.Object).AddSingleton(_recovery.Object).AddSingleton(new Mock<IEventDispatcher>().Object);

        var app = builder.Build();
        app.UseAuthentication();
        app.MapSencillaEndpoints();
        app.MapGet("/whoami", (HttpContext http) => http.User.FindFirst(AuthClaims.Subject)?.Value ?? "anonymous");
        await app.StartAsync();
        return app;
    }
}

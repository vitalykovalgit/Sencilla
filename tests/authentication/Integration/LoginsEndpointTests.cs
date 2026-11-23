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
/// The ways-in endpoints over a cookie session: which redirect sign-ins the app offers (anonymous), and the signed-in
/// account's own — listed, disconnected one at a time but never the last, and the confirmed-phone hint. The store is
/// mocked; the disconnect rule is the real <see cref="AccountLinkService"/>'s.
/// </summary>
public sealed class LoginsEndpointTests
{
    private readonly Mock<IUserStore> _store = new();
    private readonly Mock<IAccountRecoveryService> _recovery = new();
    private readonly AuthUser _account = new(Guid.NewGuid(), Email: null, EmailConfirmed: false, AuthUserStatus.Active);

    [Fact]
    public async Task Providers_ListsTheRedirectSignIns_InRegistrationOrder_ToAnyone()
    {
        await using var app = await StartHost(a => a
            .UseGoogle(o => { o.RedirectSignIn = true; o.ClientId = "google-id"; o.ClientSecret = "google-secret"; })
            .UseTelegram(o => { o.ClientId = "123456789"; o.ClientSecret = "telegram-secret"; }));

        var providers = await app.GetTestClient().GetStringAsync("/api/v1/auth/providers");

        Assert.Equal("""[{"provider":"google","scheme":"Google"},{"provider":"telegram","scheme":"Telegram"}]""", providers);
    }

    [Fact]
    public async Task Providers_WithoutARedirectSignIn_IsEmpty()
    {
        await using var app = await StartHost(a => a.UseGoogle());

        Assert.Equal("[]", await app.GetTestClient().GetStringAsync("/api/v1/auth/providers"));
    }

    [Fact]
    public async Task Logins_AnswerTheAccountsProviders_AndWhetherItHasAPassword_OnlyToItsSession()
    {
        LinkedTo(["google", "telegram"], hasPassword: true);
        await using var app = await StartHost();
        var client = app.GetTestClient();
        var session = await SignIn(client);

        var logins = await client.SendAsync(WithTheCookieOf(session, new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/logins")));
        var anonymous = await client.GetAsync("/api/v1/auth/logins");

        Assert.Equal("""{"providers":["google","telegram"],"hasPassword":true}""", await logins.Content.ReadAsStringAsync());
        Assert.NotEqual(HttpStatusCode.OK, anonymous.StatusCode);
    }

    [Fact]
    public async Task Disconnect_OfTheOnlyWayIn_IsRefused()
    {
        LinkedTo(["telegram"], hasPassword: false);
        await using var app = await StartHost();
        var client = app.GetTestClient();
        var session = await SignIn(client);

        var refused = await client.SendAsync(WithTheCookieOf(session, new HttpRequestMessage(HttpMethod.Delete, "/api/v1/auth/logins/telegram")));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("""{"error":"last-sign-in-method"}""", await refused.Content.ReadAsStringAsync());
        _store.Verify(s => s.UnlinkProvider(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("google", new[] { "google", "telegram" }, false)]   // another provider remains
    [InlineData("google", new[] { "google" }, true)]                // the password remains
    [InlineData("facebook", new[] { "google" }, false)]             // not linked at all
    public async Task Disconnect_WhileAnotherWayInRemains_RemovesIt(string provider, string[] linked, bool hasPassword)
    {
        LinkedTo(linked, hasPassword);
        await using var app = await StartHost();
        var client = app.GetTestClient();
        var session = await SignIn(client);

        var removed = await client.SendAsync(WithTheCookieOf(session, new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/auth/logins/{provider}")));

        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        _store.Verify(s => s.UnlinkProvider(_account.Id, provider, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PhoneOnAnotherAccount_AnswersWhetherTheConfirmedPhoneIsTypedElsewhere(bool exists)
    {
        _store.Setup(s => s.IsConfirmedPhoneOnAnotherAccount(_account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exists);
        await using var app = await StartHost();
        var client = app.GetTestClient();
        var session = await SignIn(client);

        var hint = await client.SendAsync(WithTheCookieOf(session, new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/phone-on-another-account")));

        Assert.Equal($$"""{"exists":{{(exists ? "true" : "false")}}}""", await hint.Content.ReadAsStringAsync());
    }

    private void LinkedTo(string[] providers, bool hasPassword)
    {
        _store.Setup(s => s.GetLinkedProviders(_account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(providers);
        _store.Setup(s => s.HasPasswordCredential(_account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(hasPassword);
    }

    /// <summary>A cookie session for the account, started by the reset endpoint over the faked recovery service.</summary>
    private async Task<HttpResponseMessage> SignIn(HttpClient client)
    {
        _recovery.Setup(r => r.ResetPassword("reset-token", "N3wSecret!!", It.IsAny<CancellationToken>())).ReturnsAsync(_account);
        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token = "reset-token", newPassword = "N3wSecret!!" });
        reset.EnsureSuccessStatusCode();
        return reset;
    }

    private static HttpRequestMessage WithTheCookieOf(HttpResponseMessage signIn, HttpRequestMessage request)
    {
        request.Headers.Add("Cookie", RedirectSignInHost.CookiesOf(signIn));
        return request;
    }

    private async Task<WebApplication> StartHost(Action<AuthenticationConfig>? providers = null)
    {
        _store.Setup(s => s.GetRoles(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        builder.Services.AddAuthorization();
        builder.Services.AddSencillaAuthentication(builder.Configuration, a =>
        {
            a.UseCookieSession().UseAppAccounts().UseUsers(u => u.EntityFrameworkStore());
            providers?.Invoke(a);
        });
        // Registered after the family, so the fakes win over its store and recovery service.
        builder.Services.AddSingleton(_store.Object).AddSingleton(_recovery.Object).AddSingleton(new Mock<IEventDispatcher>().Object);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSencillaEndpoints();
        await app.StartAsync();
        return app;
    }
}

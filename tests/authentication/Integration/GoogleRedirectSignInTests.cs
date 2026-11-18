using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;

namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// The Google redirect sign-in end to end through ASP.NET's real Google handler on a <c>TestServer</c>:
/// challenge → <c>/signin-google</c> callback, with only Google's token and user-info calls faked on the
/// backchannel. Proves the session is bound to the resolved account, and that a refusal from the
/// resolver issues no session.
/// </summary>
public sealed class GoogleRedirectSignInTests
{
    private const string SignInFailedUrl = "https://app.test/login";

    private readonly Mock<IUserStore> _store = new();

    [Fact]
    public async Task NewGoogleUser_IsSignedIntoTheCookie_AsTheCreatedAccount()
    {
        AuthUser? created = null;
        ExternalIdentity? google = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity i, CancellationToken _) => { google = i; return created = u; });
        await using var app = await StartHost(
            """{"sub":"g-1","email":"new@example.com","email_verified":true,"name":"Anna Maria Koval","given_name":"Anna Maria","family_name":"Koval"}""");
        var client = app.GetTestClient();

        var callback = await SignInWithGoogle(client);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Contains(callback.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".AspNetCore.Cookies="));
        var whoami = new HttpRequestMessage(HttpMethod.Get, "/whoami");
        whoami.Headers.Add("Cookie", CookiesOf(callback));
        Assert.Equal(created!.Id.ToString(), await (await client.SendAsync(whoami)).Content.ReadAsStringAsync());
        Assert.Equal(("Anna Maria", "Koval"), (google!.GivenName, google.FamilyName));
    }

    [Fact]
    public async Task RefusedIdentity_IssuesNoSession_AndLandsOnSignInFailedUrl()
    {
        // An unverified Google address that is somebody else's account.
        _store.Setup(s => s.FindByEmail("owner@example.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AuthUser(Guid.NewGuid(), "owner@example.com", true, AuthUserStatus.Active));
        await using var app = await StartHost("""{"sub":"g-2","email":"owner@example.com","email_verified":false}""");

        var callback = await SignInWithGoogle(app.GetTestClient());

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.DoesNotContain(callback.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [], c => c.StartsWith(".AspNetCore.Cookies"));
        Assert.Equal($"{SignInFailedUrl}?error=email-not-verified", callback.Headers.Location!.ToString());
        _store.Verify(s => s.LinkProvider(It.IsAny<Guid>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<WebApplication> StartHost(string googleUserJson)
    {
        // The session's principal is built for the account, roles included.
        _store.Setup(s => s.GetRoles(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddSingleton(_store.Object).AddSingleton(new Mock<IEventDispatcher>().Object);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        builder.Services.AddSencillaAuthentication(builder.Configuration, a => a
            .UseGoogle(o => { o.ClientId = "client-id"; o.ClientSecret = "client-secret"; o.RedirectSignIn = true; o.SignInFailedUrl = SignInFailedUrl; })
            .UseApi(o => o.Disabled = true));
        builder.Services.Configure<GoogleOptions>(GoogleDefaults.AuthenticationScheme, o => o.BackchannelHttpHandler = new FakeGoogle(googleUserJson));

        var app = builder.Build();
        app.UseAuthentication();
        app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [GoogleDefaults.AuthenticationScheme]));
        app.MapGet("/whoami", (HttpContext http) => http.User.FindFirst(AuthClaims.Subject)?.Value ?? "anonymous");
        await app.StartAsync();
        return app;
    }

    /// <summary>What a browser does: follow the challenge to Google and come back with its <c>state</c>, a code and the correlation cookie.</summary>
    private static async Task<HttpResponseMessage> SignInWithGoogle(HttpClient client)
    {
        var challenge = await client.GetAsync("/login");
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();

        var callback = new HttpRequestMessage(HttpMethod.Get, $"/signin-google?code=fake-code&state={Uri.EscapeDataString(state)}");
        callback.Headers.Add("Cookie", CookiesOf(challenge));
        return await client.SendAsync(callback);
    }

    private static string CookiesOf(HttpResponseMessage response)
        => string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));

    /// <summary>Google's token endpoint (POST) and user-info endpoint (GET).</summary>
    private sealed class FakeGoogle(string userJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    request.Method == HttpMethod.Post ? """{"access_token":"fake-access","token_type":"Bearer","expires_in":3600}""" : userJson,
                    Encoding.UTF8, "application/json"),
            });
    }
}

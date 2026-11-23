using System.Net;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// The Google redirect sign-in end to end through ASP.NET's real Google handler on a <c>TestServer</c>:
/// challenge → <c>/signin-google</c> callback, with only Google's token and user-info calls faked on the
/// backchannel. Proves the session is bound to the resolved account, that a refusal from the resolver issues no
/// session, and — Google standing in for every provider on the shared binding — where a failure and a connect land.
/// </summary>
public sealed class GoogleRedirectSignInTests
{
    private const string NewUser = """{"sub":"g-1","email":"new@example.com","email_verified":true,"name":"Anna Maria Koval","given_name":"Anna Maria","family_name":"Koval"}""";

    private readonly Mock<IUserStore> _store = new();

    [Fact]
    public async Task NewGoogleUser_IsSignedIntoTheCookie_AsTheCreatedAccount()
    {
        AuthUser? created = null;
        ExternalIdentity? google = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity i, CancellationToken _) => { google = i; return created = u; });
        await using var app = await StartHost(NewUser);
        var client = app.GetTestClient();

        var (_, callback) = await SignInWithGoogle(client);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.True(RedirectSignInHost.IssuesASession(callback));
        Assert.Equal(created!.Id.ToString(), await RedirectSignInHost.WhoIsSignedInBy(client, callback));
        Assert.Equal(("Anna Maria", "Koval"), (google!.GivenName, google.FamilyName));
        Assert.True(google.EmailVerified);
    }

    [Fact]
    public async Task RefusedIdentity_IssuesNoSession_AndLandsOnSignInFailedUrl()
    {
        // An unverified Google address that is somebody else's account.
        _store.Setup(s => s.FindByEmail("owner@example.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AuthUser(Guid.NewGuid(), "owner@example.com", true, AuthUserStatus.Active));
        await using var app = await StartHost("""{"sub":"g-2","email":"owner@example.com","email_verified":false}""");

        var (_, callback) = await SignInWithGoogle(app.GetTestClient());

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.False(RedirectSignInHost.IssuesASession(callback));
        Assert.Equal($"{RedirectSignInHost.SignInFailedUrl}?error=email-not-verified", callback.Headers.Location!.ToString());
        _store.Verify(s => s.LinkProvider(It.IsAny<Guid>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeniedAtTheProvider_LandsOnSignInFailedUrl_AsGoogleFailed()
    {
        await using var app = await StartHost(NewUser);

        var (_, callback) = await SignInWithGoogle(app.GetTestClient(), answer: "error=access_denied");

        Assert.Equal($"{RedirectSignInHost.SignInFailedUrl}?error=google-failed", callback.Headers.Location!.ToString());
        Assert.False(RedirectSignInHost.IssuesASession(callback));
    }

    [Fact]
    public async Task AFailureBindingTheAccount_LandsOnSignInFailedUrl_AsGoogleFailed()
    {
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("the database is down"));
        await using var app = await StartHost(NewUser);

        var (_, callback) = await SignInWithGoogle(app.GetTestClient());

        Assert.Equal($"{RedirectSignInHost.SignInFailedUrl}?error=google-failed", callback.Headers.Location!.ToString());
        Assert.False(RedirectSignInHost.IssuesASession(callback));
    }

    [Fact]
    public async Task Connect_AttachesGoogleToTheSessionsAccount_SignsItIn_AndReturnsWhereTheChallengeAsked()
    {
        var account = new AuthUser(Guid.NewGuid(), "me@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _store.Setup(s => s.FindById(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _store.Setup(s => s.GetLinkedProviders(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(["telegram"]);
        await using var app = await StartHost(NewUser);
        var client = app.GetTestClient();

        var (_, callback) = await SignInWithGoogle(client, $"/connect/Google/{account.Id}");

        Assert.Equal(RedirectSignInHost.ProfileUrl, callback.Headers.Location!.ToString());
        Assert.Equal(account.Id.ToString(), await RedirectSignInHost.WhoIsSignedInBy(client, callback));
        _store.Verify(s => s.LinkProvider(account.Id, It.Is<ExternalIdentity>(i => i.Subject == "g-1"), It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Connect_OfAnotherAccountsGoogle_ReturnsWhereTheChallengeAsked_WithTheCode_AndNoSession()
    {
        var account = new AuthUser(Guid.NewGuid(), "me@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _store.Setup(s => s.FindById(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _store.Setup(s => s.FindByProviderKey("google", "g-1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AuthUser(Guid.NewGuid(), "new@example.com", true, AuthUserStatus.Active));
        await using var app = await StartHost(NewUser);

        var (_, callback) = await SignInWithGoogle(app.GetTestClient(), $"/connect/Google/{account.Id}");

        Assert.Equal($"{RedirectSignInHost.ProfileUrl}?error=provider-linked-to-another-account", callback.Headers.Location!.ToString());
        Assert.False(RedirectSignInHost.IssuesASession(callback));
    }

    [Fact]
    public async Task Connect_DeniedAtTheProvider_ReturnsWhereTheChallengeAsked_AsGoogleFailed()
    {
        await using var app = await StartHost(NewUser);

        var (_, callback) = await SignInWithGoogle(app.GetTestClient(), $"/connect/Google/{Guid.NewGuid()}", answer: "error=access_denied");

        Assert.Equal($"{RedirectSignInHost.ProfileUrl}?error=google-failed", callback.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AStateThatCannotBeRead_LandsOnSignInFailedUrl_AsGoogleFailed()
    {
        await using var app = await StartHost(NewUser);

        var callback = await app.GetTestClient().GetAsync("/signin-google?code=fake-code&state=forged");

        Assert.Equal($"{RedirectSignInHost.SignInFailedUrl}?error=google-failed", callback.Headers.Location!.ToString());
    }

    private Task<WebApplication> StartHost(string googleUserJson) => RedirectSignInHost.Start(
        _store,
        a => a.UseGoogle(o => { o.ClientId = "client-id"; o.ClientSecret = "client-secret"; o.RedirectSignIn = true; o.SignInFailedUrl = RedirectSignInHost.SignInFailedUrl; }),
        s => s.Configure<GoogleOptions>(GoogleDefaults.AuthenticationScheme, o => o.BackchannelHttpHandler = new FakeGoogle(googleUserJson)));

    private static Task<(HttpResponseMessage Challenge, HttpResponseMessage Callback)> SignInWithGoogle(
        HttpClient client, string challenge = "/login/Google", string answer = "code=fake-code")
        => RedirectSignInHost.ComeBackFromProvider(client, challenge, "/signin-google", answer);

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

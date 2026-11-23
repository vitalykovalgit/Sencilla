using System.Net;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;

namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// Business Login for Instagram end to end through ASP.NET's generic OAuth handler with Instagram's two steps: the
/// code exchange and <c>/me</c> both answer as Meta's samples do, wrapped in <c>data</c>, faked on the back channel.
/// </summary>
public sealed class InstagramRedirectSignInTests
{
    private readonly Mock<IUserStore> _store = new();

    [Fact]
    public async Task NewUser_GetsAnAccountNamedByTheirUsername_WithNoContact()
    {
        AuthUser? created = null;
        ExternalIdentity? instagram = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity i, CancellationToken _) => { instagram = i; return created = u; });
        var provider = new FakeInstagram();
        await using var app = await RedirectSignInHost.Start(
            _store,
            a => a.UseInstagram(o => { o.ClientId = "990602627938098"; o.ClientSecret = "client-secret"; o.SignInFailedUrl = RedirectSignInHost.SignInFailedUrl; }),
            s => s.Configure<OAuthOptions>("Instagram", o => o.BackchannelHttpHandler = provider));
        var client = app.GetTestClient();

        var (challenge, callback) = await RedirectSignInHost.ComeBackFromProvider(client, "/login/Instagram", "/signin-instagram");

        Assert.StartsWith("https://www.instagram.com/oauth/authorize?", challenge.Headers.Location!.ToString());
        Assert.Equal("instagram_business_basic", QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["scope"].ToString());
        Assert.Equal(created!.Id.ToString(), await RedirectSignInHost.WhoIsSignedInBy(client, callback));
        Assert.Equal(new ExternalIdentity("instagram", "17841400000", Email: null, EmailVerified: false, "ada.prints", "https://ig/ada.jpg"), instagram);
        Assert.Equal("fake-access", QueryHelpers.ParseQuery(provider.ProfileRequest!.Query)["access_token"].ToString());
    }

    /// <summary>Instagram's code exchange (POST) and the Graph host's <c>/me</c> (GET).</summary>
    private sealed class FakeInstagram : HttpMessageHandler
    {
        public Uri? ProfileRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method == HttpMethod.Get)
                ProfileRequest = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(request.Method == HttpMethod.Post
                    ? """{"data":[{"access_token":"fake-access","user_id":"17841400000","permissions":"instagram_business_basic"}]}"""
                    : """{"data":[{"user_id":"17841400000","username":"ada.prints","profile_picture_url":"https://ig/ada.jpg"}]}""",
                    Encoding.UTF8, "application/json"),
            });
        }
    }
}

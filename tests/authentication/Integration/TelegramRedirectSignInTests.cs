using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// Log In With Telegram end to end through ASP.NET's real OpenID Connect handler: the challenge asks for a code with
/// PKCE and Telegram's scopes, and the callback redeems it at a faked token endpoint whose <c>id_token</c> is signed
/// with a key generated for the run. Proves the handler's wiring the reader depends on — raw claim names, the granted
/// scope carried onto the principal — down to the account and the identity the host is told about.
/// </summary>
public sealed class TelegramRedirectSignInTests
{
    private const string Issuer = "https://oauth.telegram.org";
    private const string BotId = "123456789";

    private static readonly RsaSecurityKey TelegramKey = new(RSA.Create(2048)) { KeyId = "telegram-test" };

    private readonly Mock<IUserStore> _store = new();
    private readonly List<IEvent> _events = [];

    [Fact]
    public async Task NewUserSharingThePhone_AndLettingTheBotWrite_GetsAConfirmedPhone_AndTheBotsChat()
    {
        AuthUser? created = null;
        ExternalIdentity? telegram = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity i, CancellationToken _) => { telegram = i; return created = u; });
        var provider = new FakeTelegram(grantedScope: "openid profile phone telegram:bot_access");
        await using var app = await StartHost(provider);
        var client = app.GetTestClient();

        var (challenge, callback) = await RedirectSignInHost.ComeBackFromProvider(client, "/login/Telegram", "/signin-telegram");

        var asked = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
        Assert.StartsWith($"{Issuer}/auth?", challenge.Headers.Location!.ToString());
        Assert.Equal(("code", "S256", BotId), (asked["response_type"].ToString(), asked["code_challenge_method"].ToString(), asked["client_id"].ToString()));
        Assert.Equal("openid profile phone telegram:bot_access", asked["scope"].ToString());
        Assert.Contains("code_verifier=", provider.TokenRequest);

        Assert.Equal(created!.Id.ToString(), await RedirectSignInHost.WhoIsSignedInBy(client, callback));
        Assert.Equal(("1234123412341234123", 971577777777L, true), (telegram!.Subject, telegram.Phone, telegram.PhoneVerified));
        Assert.Equal("987654321", telegram.RawClaims![TelegramRawClaims.BotChatId]);
        Assert.Equal("987654321", _events.OfType<LoginSucceeded>().Single().ExternalIdentity!.RawClaims![TelegramRawClaims.BotChatId]);
    }

    [Fact]
    public async Task UserRefusingTheBot_IsSignedIn_WithoutAChat()
    {
        ExternalIdentity? telegram = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity i, CancellationToken _) => { telegram = i; return u; });
        await using var app = await StartHost(new FakeTelegram(grantedScope: "openid profile phone"));

        var (_, callback) = await RedirectSignInHost.ComeBackFromProvider(app.GetTestClient(), "/login/Telegram", "/signin-telegram");

        Assert.True(RedirectSignInHost.IssuesASession(callback));
        Assert.Null(telegram!.RawClaims);
    }

    [Fact]
    public async Task RemoteSignOut_IsNotMapped()
    {
        await using var app = await StartHost(new FakeTelegram(grantedScope: null));

        var signOut = await app.GetTestClient().GetAsync("/signout-oidc");

        Assert.Equal(HttpStatusCode.NotFound, signOut.StatusCode);
    }

    [Fact]
    public async Task SignIn_Completes_WhenTelegramAnswersGzipUnasked()
    {
        // Telegram's edge gzips its JWKS (and may its discovery document) whether asked or not. Here a real HTTP server
        // does so for all three calls, so they go through the back channel the package builds, not a faked handler.
        AuthUser? created = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity _, CancellationToken _) => created = u);
        await using var telegram = await StartTelegramAnsweringGzip();
        await using var app = await RedirectSignInHost.Start(
            _store,
            a => a.UseTelegram(o => { o.ClientId = BotId; o.ClientSecret = "client-secret"; o.SignInFailedUrl = RedirectSignInHost.SignInFailedUrl; }),
            s => s.Configure<OpenIdConnectOptions>("Telegram", o =>
            {
                o.MetadataAddress = $"{telegram.Urls.Single()}/.well-known/openid-configuration";
                o.RequireHttpsMetadata = false;
            }));
        var client = app.GetTestClient();

        var (_, callback) = await RedirectSignInHost.ComeBackFromProvider(client, "/login/Telegram", "/signin-telegram");

        Assert.Equal(created!.Id.ToString(), await RedirectSignInHost.WhoIsSignedInBy(client, callback));
    }

    /// <summary>Telegram's discovery document, JWKS and token endpoint on a local port, every answer gzip-encoded.</summary>
    private static async Task<WebApplication> StartTelegramAnsweringGzip()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var telegram = builder.Build();
        var key = TelegramKey.Rsa.ExportParameters(includePrivateParameters: false);
        telegram.MapGet("/.well-known/openid-configuration", (HttpContext http) =>
        {
            var self = $"{http.Request.Scheme}://{http.Request.Host}";
            return AnswerGzipped(http, $$"""{"issuer":"{{Issuer}}","authorization_endpoint":"{{self}}/auth","token_endpoint":"{{self}}/token","jwks_uri":"{{self}}/.well-known/jwks.json"}""");
        });
        telegram.MapGet("/.well-known/jwks.json", (HttpContext http) => AnswerGzipped(http,
            $$"""{"keys":[{"kty":"RSA","use":"sig","alg":"RS256","kid":"{{TelegramKey.KeyId}}","n":"{{Base64UrlEncoder.Encode(key.Modulus)}}","e":"{{Base64UrlEncoder.Encode(key.Exponent)}}"}]}"""));
        telegram.MapPost("/token", async (HttpContext http) =>
        {
            using var answer = await new FakeTelegram(grantedScope: null).SendAsync(new HttpRequestMessage(HttpMethod.Post, "/token") { Content = new StringContent("") });
            await AnswerGzipped(http, await answer.Content.ReadAsStringAsync());
        });
        await telegram.StartAsync();
        return telegram;
    }

    private static async Task AnswerGzipped(HttpContext http, string json)
    {
        http.Response.ContentType = "application/json";
        http.Response.Headers.ContentEncoding = "gzip";
        await using var gzip = new GZipStream(http.Response.Body, CompressionLevel.Fastest, leaveOpen: true);
        await gzip.WriteAsync(Encoding.UTF8.GetBytes(json));
    }

    private Task<WebApplication> StartHost(FakeTelegram provider)
    {
        var events = new Mock<IEventDispatcher>();
        events.Setup(e => e.PublishAsync(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()))
              .Callback<IEvent, CancellationToken>((e, _) => _events.Add(e)).Returns(Task.CompletedTask);
        return RedirectSignInHost.Start(
            _store,
            a => a.UseTelegram(o => { o.ClientId = BotId; o.ClientSecret = "client-secret"; o.SignInFailedUrl = RedirectSignInHost.SignInFailedUrl; }),
            s => s.Configure<OpenIdConnectOptions>("Telegram", o =>
            {
                // Telegram's discovery document, as it answers, minus the network.
                o.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer, AuthorizationEndpoint = $"{Issuer}/auth", TokenEndpoint = $"{Issuer}/token" };
                o.Configuration.SigningKeys.Add(TelegramKey);
                o.BackchannelHttpHandler = provider;
            }),
            events.Object);
    }

    /// <summary>Telegram's token endpoint: the page's sample <c>id_token</c>, and the scope granted when the test says one was.</summary>
    private sealed class FakeTelegram(string? grantedScope) : HttpMessageHandler
    {
        public string TokenRequest { get; private set; } = string.Empty;

        public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) => SendAsync(request, CancellationToken.None);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            TokenRequest = await request.Content!.ReadAsStringAsync(token);
            var idToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = BotId,
                SigningCredentials = new SigningCredentials(TelegramKey, SecurityAlgorithms.RsaSha256),
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = "1234123412341234123",
                    ["id"] = 987654321,
                    ["name"] = "John Doe",
                    ["given_name"] = "John",
                    ["family_name"] = "Doe",
                    ["picture"] = "https://cdn4.telesco.pe/file/john.jpg",
                    ["phone_number"] = "971577777777",
                    ["phone_number_verified"] = true,
                },
            });
            var scope = grantedScope is null ? "" : $",\"scope\":\"{grantedScope}\"";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"access_token":"fake-access","token_type":"Bearer","expires_in":3600,"id_token":"{{idToken}}"{{scope}}}""", Encoding.UTF8, "application/json"),
            };
        }
    }
}

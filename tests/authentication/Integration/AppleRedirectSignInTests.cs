using System.Net;
using System.Security.Cryptography;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// Sign in with Apple end to end through <c>AspNet.Security.OAuth.Apple</c>: Apple POSTs its answer back cross-site,
/// the code is redeemed with a client secret signed by the configured key's CONTENT (a key generated for the run), and
/// the <c>id_token</c> is signed with another run-time key. Proves the posted callback finds its correlation, the name
/// Apple posts once reaches the identity, and a relay address arrives verified.
/// </summary>
public sealed class AppleRedirectSignInTests
{
    private const string ServicesId = "pro.example.signin";
    private const string TeamId = "TEAM123456";
    private const string KeyId = "KEY1234567";

    private static readonly RsaSecurityKey AppleKey = new(RSA.Create(2048)) { KeyId = "apple-test" };

    private readonly ECDsa _ourKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly Mock<IUserStore> _store = new();

    [Fact]
    public async Task NewUser_PostedBack_WithARelayAddress_GetsAVerifiedAccount_NamedAsApplePosted()
    {
        AuthUser? created = null;
        ExternalIdentity? apple = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity i, CancellationToken _) => { apple = i; return created = u; });
        var provider = new FakeApple();
        await using var app = await StartHost(provider);
        var client = app.GetTestClient();

        var (challenge, callback) = await RedirectSignInHost.ComeBackFromProvider(client, "/login/Apple", "/signin-apple",
            posted: new Dictionary<string, string> { ["user"] = """{"name":{"firstName":"Anna Maria","lastName":"Koval"},"email":"shield@privaterelay.appleid.com"}""" });

        Assert.Equal("form_post", QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["response_mode"].ToString());
        // The posted answer is a cross-site request: the correlation cookie must travel on it.
        var correlation = challenge.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(".AspNetCore.Correlation."));
        Assert.Contains("samesite=none", correlation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", correlation, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(created!.Id.ToString(), await RedirectSignInHost.WhoIsSignedInBy(client, callback));
        Assert.Equal(("apple-sub-1", "shield@privaterelay.appleid.com", true), (apple!.Subject, apple.Email, apple.EmailVerified));
        Assert.Equal("true", apple.RawClaims!["is_private_email"]);
        Assert.Equal(("Anna Maria", "Koval"), (apple.GivenName, apple.FamilyName));
        Assert.True(created.EmailConfirmed);
        await AssertSignedWithOurKey(provider.ClientSecret);
    }

    [Fact]
    public async Task ASecondSignIn_WithoutAPostedName_HasNoName()
    {
        ExternalIdentity? apple = null;
        _store.Setup(s => s.CreateWithExternalLogin(It.IsAny<AuthUser>(), It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((AuthUser u, ExternalIdentity i, CancellationToken _) => { apple = i; return u; });
        await using var app = await StartHost(new FakeApple());

        var (_, callback) = await RedirectSignInHost.ComeBackFromProvider(app.GetTestClient(), "/login/Apple", "/signin-apple",
            posted: new Dictionary<string, string>());

        Assert.True(RedirectSignInHost.IssuesASession(callback));
        Assert.Equal((null, null), (apple!.GivenName, apple.FamilyName));
    }

    /// <summary>Apple accepts a client secret only as an ES256 JWT from the team, about the Services ID, under the key id.</summary>
    private async Task AssertSignedWithOurKey(string clientSecret)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(clientSecret, new TokenValidationParameters
        {
            ValidIssuer = TeamId,
            ValidAudience = "https://appleid.apple.com",
            IssuerSigningKey = new ECDsaSecurityKey(ECDsa.Create(_ourKey.ExportParameters(includePrivateParameters: false))) { KeyId = KeyId },
        });
        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(ServicesId, ((JsonWebToken)result.SecurityToken).Subject);
        Assert.Equal(KeyId, ((JsonWebToken)result.SecurityToken).Kid);
    }

    private Task<WebApplication> StartHost(FakeApple provider) => RedirectSignInHost.Start(
        _store,
        a => a.UseApple(o =>
        {
            o.RedirectSignIn = true;
            o.ClientId = ServicesId;
            o.TeamId = TeamId;
            o.KeyId = KeyId;
            o.PrivateKey = _ourKey.ExportPkcs8PrivateKeyPem();
            o.SignInFailedUrl = RedirectSignInHost.SignInFailedUrl;
        }),
        s => s.Configure<AppleAuthenticationOptions>(AppleAuthenticationDefaults.AuthenticationScheme, o =>
        {
            var keys = new OpenIdConnectConfiguration { JsonWebKeySet = new JsonWebKeySet() };
            keys.JsonWebKeySet.Keys.Add(JsonWebKeyConverter.ConvertFromRSASecurityKey(AppleKey));
            o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(keys);
            o.BackchannelHttpHandler = provider;
        }));

    /// <summary>Apple's token endpoint: a relay-address <c>id_token</c>, with <c>email_verified</c> as a string, as Apple sends it.</summary>
    private sealed class FakeApple : HttpMessageHandler
    {
        public string ClientSecret { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            ClientSecret = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(token))["client_secret"].ToString();
            var idToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "https://appleid.apple.com",
                Audience = ServicesId,
                SigningCredentials = new SigningCredentials(AppleKey, SecurityAlgorithms.RsaSha256),
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = "apple-sub-1",
                    ["email"] = "shield@privaterelay.appleid.com",
                    ["email_verified"] = "true",
                    ["is_private_email"] = "true",
                },
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"access_token":"fake-access","token_type":"Bearer","expires_in":3600,"id_token":"{{idToken}}"}""", Encoding.UTF8, "application/json"),
            };
        }
    }
}

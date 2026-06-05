namespace Sencilla.Authentication.Providers.Tests;

public class ProviderTokenVerifierTests
{
    private static readonly RSA Rsa = RSA.Create(2048);
    private static readonly RsaSecurityKey SigningKey = new(Rsa) { KeyId = "test-kid" };

    private const string GoogleIssuer = "https://accounts.google.com";
    private const string AppleIssuer = "https://appleid.apple.com";
    private const string FacebookIssuer = "https://www.facebook.com";

    private static StaticConfigurationManager<OpenIdConnectConfiguration> Config(string issuer, SecurityKey key)
    {
        var config = new OpenIdConnectConfiguration { Issuer = issuer };
        config.SigningKeys.Add(key);
        return new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
    }

    private static string Mint(SecurityKey key, string issuer, string audience, IDictionary<string, object> claims, DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var exp = expires ?? now.AddMinutes(5);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
            IssuedAt = exp.AddMinutes(-5),
            NotBefore = exp.AddMinutes(-5),
            Expires = exp,
            Claims = claims,
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static GoogleTokenVerifier Google() => new(Options.Create(
        new GoogleProviderOptions
        {
            Audiences = { "client-1" },
            Issuers = new List<string> { GoogleIssuer },
            ConfigurationManager = Config(GoogleIssuer, SigningKey),
        }));

    private static Dictionary<string, object> GoogleClaims() => new()
    {
        ["sub"] = "google-123",
        ["email"] = "ada@example.com",
        ["email_verified"] = true,
        ["name"] = "Ada Lovelace",
        ["picture"] = "https://pic/ada.png",
    };

    [Fact]
    public async Task Google_ValidToken_MapsIdentity()
    {
        var token = Mint(SigningKey, GoogleIssuer, "client-1", GoogleClaims());

        var id = await Google().Verify(token);

        Assert.Equal("google", id.Provider);
        Assert.Equal("google-123", id.Subject);
        Assert.Equal("ada@example.com", id.Email);
        Assert.True(id.EmailVerified);
        Assert.Equal("Ada Lovelace", id.Name);
        Assert.Equal("https://pic/ada.png", id.Picture);
    }

    [Fact]
    public async Task Google_WrongAudience_Throws()
    {
        var token = Mint(SigningKey, GoogleIssuer, "someone-elses-client", GoogleClaims());
        await Assert.ThrowsAsync<UnauthorizedException>(() => Google().Verify(token));
    }

    [Fact]
    public async Task Google_WrongIssuer_Throws()
    {
        var token = Mint(SigningKey, "https://evil.example.com", "client-1", GoogleClaims());
        await Assert.ThrowsAsync<UnauthorizedException>(() => Google().Verify(token));
    }

    [Fact]
    public async Task Google_BadSignature_Throws()
    {
        using var otherRsa = RSA.Create(2048);
        var otherKey = new RsaSecurityKey(otherRsa) { KeyId = "intruder" };
        var token = Mint(otherKey, GoogleIssuer, "client-1", GoogleClaims());
        await Assert.ThrowsAsync<UnauthorizedException>(() => Google().Verify(token));
    }

    [Fact]
    public async Task Google_Expired_Throws()
    {
        var token = Mint(SigningKey, GoogleIssuer, "client-1", GoogleClaims(), expires: DateTime.UtcNow.AddMinutes(-10));
        await Assert.ThrowsAsync<UnauthorizedException>(() => Google().Verify(token));
    }

    [Fact]
    public async Task Apple_RelayEmail_StringVerified_NoName_Maps()
    {
        var verifier = new AppleTokenVerifier(Options.Create(
            new AppleProviderOptions
            {
                Audiences = { "app.bundle.id" },
                Issuers = new List<string> { AppleIssuer },
                ConfigurationManager = Config(AppleIssuer, SigningKey),
            }));

        var token = Mint(SigningKey, AppleIssuer, "app.bundle.id", new Dictionary<string, object>
        {
            ["sub"] = "apple-sub-9",
            ["email"] = "shield@privaterelay.appleid.com",
            ["email_verified"] = "true",   // Apple sends this as a string
            ["is_private_email"] = "true",
        });

        var id = await verifier.Verify(token);

        Assert.Equal("apple-sub-9", id.Subject);
        Assert.Equal("shield@privaterelay.appleid.com", id.Email);
        Assert.True(id.EmailVerified);
        Assert.Null(id.Name);
        Assert.Equal("true", id.RawClaims!["is_private_email"]);
    }

    [Fact]
    public async Task Facebook_NoEmail_MapsWithUnverifiedEmail()
    {
        var verifier = new FacebookTokenVerifier(Options.Create(
            new FacebookProviderOptions
            {
                Audiences = { "fb-app-id" },
                Issuers = new List<string> { FacebookIssuer },
                ConfigurationManager = Config(FacebookIssuer, SigningKey),
            }));

        var token = Mint(SigningKey, FacebookIssuer, "fb-app-id", new Dictionary<string, object>
        {
            ["sub"] = "fb-sub-3",
            ["name"] = "No Email User",
        });

        var id = await verifier.Verify(token);

        Assert.Equal("facebook", id.Provider);
        Assert.Equal("fb-sub-3", id.Subject);
        Assert.Null(id.Email);
        Assert.False(id.EmailVerified);
    }
}

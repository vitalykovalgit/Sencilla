using System.Security.Claims;
using AspNet.Security.OAuth.Apple;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.Authentication.Providers.Tests;

/// <summary>
/// Each provider's reader of its redirect sign-in, from a canned answer — the user-info JSON through the handler's
/// claim actions as the provider package configures them, or the <c>id_token</c>'s claims — to the
/// <see cref="ExternalIdentity"/> the shared binding resolves.
/// </summary>
public class RedirectSignInReaderTests
{
    [Fact]
    public void Google_ReadsTheVerifiedAddress_TheNameParts_AndThePicture()
    {
        var options = new GoogleOptions();
        GoogleRedirectSignIn.Configure(options, new GoogleProviderOptions { ClientId = "id", ClientSecret = "secret" });

        var google = GoogleRedirectSignIn.ReadIdentity(ClaimsOf(options.ClaimActions,
            """{"sub":"g-1","email":"ada@example.com","email_verified":true,"name":"Ada Lovelace","given_name":"Augusta Ada","family_name":"Lovelace","picture":"https://pic/ada.png"}"""));

        Assert.Equal(new ExternalIdentity("google", "g-1", "ada@example.com", EmailVerified: true, "Ada Lovelace", "https://pic/ada.png",
            GivenName: "Augusta Ada", FamilyName: "Lovelace"), google);
    }

    [Fact]
    public void Facebook_NeverVerifiesItsAddress()
    {
        var options = new FacebookOptions();
        FacebookRedirectSignIn.Configure(options, new FacebookProviderOptions { ClientId = "app", ClientSecret = "secret" });

        var facebook = FacebookRedirectSignIn.ReadIdentity(ClaimsOf(options.ClaimActions,
            """{"id":"fb-1","email":"ada@example.com","name":"Ada Lovelace","first_name":"Ada","last_name":"Lovelace","picture":{"data":{"url":"https://fb/ada.jpg"}}}"""));

        Assert.Equal(new ExternalIdentity("facebook", "fb-1", "ada@example.com", EmailVerified: false, "Ada Lovelace", "https://fb/ada.jpg",
            GivenName: "Ada", FamilyName: "Lovelace"), facebook);
        Assert.Contains("picture", options.Fields);
    }

    [Fact]
    public void Telegram_WithThePhoneAndTheBot_ReadsAConfirmedPhone_AndTheChatTheBotMayWriteTo()
    {
        var telegram = TelegramRedirectSignIn.ReadIdentity(ClaimsOfIdToken(TelegramIdToken, grantedScope: "openid profile phone telegram:bot_access"));

        Assert.Equal("telegram", telegram.Provider);
        Assert.Equal("1234123412341234123", telegram.Subject);
        Assert.Equal((971577777777L, true), (telegram.Phone, telegram.PhoneVerified));
        Assert.Equal(("John Doe", "John", "Doe"), (telegram.Name, telegram.GivenName, telegram.FamilyName));
        Assert.Equal("https://cdn4.telesco.pe/file/john.jpg", telegram.Picture);
        Assert.Equal((null, false), (telegram.Email, telegram.EmailVerified));
        // The user's Telegram id, not `sub`.
        Assert.Equal("987654321", telegram.RawClaims![TelegramRawClaims.BotChatId]);
    }

    [Theory]
    [InlineData("openid profile phone")]
    [InlineData(null)]
    public void Telegram_WithoutTheBotsPermission_CarriesNoChat(string? grantedScope)
    {
        var telegram = TelegramRedirectSignIn.ReadIdentity(ClaimsOfIdToken(TelegramIdToken, grantedScope));

        Assert.Null(telegram.RawClaims);
    }

    [Fact]
    public void Telegram_WithoutThePhone_HasNoContact()
    {
        var telegram = TelegramRedirectSignIn.ReadIdentity(ClaimsOfIdToken("""{"sub":"tg-2","id":5,"name":"No Phone"}""", grantedScope: null));

        Assert.Equal((null, false, null), (telegram.Phone, telegram.PhoneVerified, telegram.Email));
    }

    [Fact]
    public void Apple_ARelayAddress_IsAVerifiedAddress_AndThePostedNameIsKept()
    {
        var claims = ClaimsOfIdToken("""{"sub":"apple-1","email":"shield@privaterelay.appleid.com","email_verified":"true","is_private_email":"true"}""", grantedScope: null);
        var identity = (ClaimsIdentity)claims.Identity!;
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "apple-1"));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, "Anna Maria"));
        identity.AddClaim(new Claim(ClaimTypes.Surname, "Koval"));

        var apple = AppleRedirectSignIn.ReadIdentity(claims);

        Assert.Equal(("apple-1", "shield@privaterelay.appleid.com", true), (apple.Subject, apple.Email, apple.EmailVerified));
        Assert.Equal("true", apple.RawClaims!["is_private_email"]);
        Assert.Equal(("Anna Maria", "Koval"), (apple.GivenName, apple.FamilyName));
    }

    [Theory]
    [InlineData("pem")]
    [InlineData("pem with literal \\n")]
    [InlineData("base64 body")]
    public async Task Apple_LoadsThePrivateKey_InEveryShapeAOneLineEnvValueTakes(string shape)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pem = key.ExportPkcs8PrivateKeyPem();
        var configured = shape switch
        {
            "pem" => pem,
            "pem with literal \\n" => pem.Replace("\n", "\\n"),
            _ => Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
        };
        var options = new AppleAuthenticationOptions();
        AppleRedirectSignIn.Configure(options, new AppleProviderOptions { ClientId = "services.id", TeamId = "team", KeyId = "kid", PrivateKey = configured });

        using var loaded = ECDsa.Create();
        loaded.ImportFromPem((await options.PrivateKey!("kid", CancellationToken.None)).Span);

        Assert.Equal(key.ExportSubjectPublicKeyInfo(), loaded.ExportSubjectPublicKeyInfo());
    }

    [Fact]
    public void Instagram_ReadsTheUsernameAndPicture_AndNoContact()
    {
        var options = new OAuthOptions();
        InstagramRedirectSignIn.Configure(options, new InstagramProviderOptions { ClientId = "app", ClientSecret = "secret" });
        using var me = JsonDocument.Parse("""{"data":[{"user_id":"17841400000","username":"ada.prints","profile_picture_url":"https://ig/ada.jpg"}]}""");

        var instagram = InstagramRedirectSignIn.ReadIdentity(ClaimsOf(options.ClaimActions, InstagramRedirectSignIn.FirstEntryOf(me.RootElement)));

        Assert.Equal(new ExternalIdentity("instagram", "17841400000", Email: null, EmailVerified: false, "ada.prints", "https://ig/ada.jpg"), instagram);
    }

    [Fact]
    public void ARedirectSignIn_WithoutItsKeys_FailsAtStartup_NamingWhatIsMissing()
    {
        var telegram = Assert.Throws<InvalidOperationException>(() => Start(a => a.UseTelegram()));
        var apple = Assert.Throws<InvalidOperationException>(() => Start(a => a.UseApple(o => { o.RedirectSignIn = true; o.ClientId = "services.id"; })));

        Assert.Contains("ClientId, ClientSecret", telegram.Message);
        Assert.Contains("TeamId, KeyId, PrivateKey", apple.Message);
    }

    private const string TelegramIdToken =
        """{"iss":"https://oauth.telegram.org","aud":"123456789","sub":"1234123412341234123","id":987654321,"name":"John Doe","given_name":"John","family_name":"Doe","preferred_username":"johndoe","picture":"https://cdn4.telesco.pe/file/john.jpg","phone_number":"971577777777","phone_number_verified":true}""";

    private static void Start(Action<AuthenticationConfig> configure)
        => new ServiceCollection().AddSencillaAuthentication(new ConfigurationBuilder().Build(), a => configure(a.UseApi(o => o.Disabled = true)));

    private static ClaimsPrincipal ClaimsOf(ClaimActionCollection actions, string userJson)
    {
        using var user = JsonDocument.Parse(userJson);
        return ClaimsOf(actions, user.RootElement);
    }

    private static ClaimsPrincipal ClaimsOf(ClaimActionCollection actions, JsonElement user)
    {
        var identity = new ClaimsIdentity("test");
        foreach (var action in actions)
            action.Run(user, identity, "test");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>The claims an <c>id_token</c> with this payload carries, as the handler reads them, plus the scope the token response granted.</summary>
    private static ClaimsPrincipal ClaimsOfIdToken(string payload, string? grantedScope)
    {
        var identity = new ClaimsIdentity(new JsonWebToken(new JsonWebTokenHandler().CreateToken(payload)).Claims, "test");
        if (grantedScope is not null)
            identity.AddClaim(new Claim("scope", grantedScope));
        return new ClaimsPrincipal(identity);
    }
}

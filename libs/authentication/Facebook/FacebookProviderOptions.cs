namespace Sencilla.Authentication;

/// <summary>
/// Facebook configuration, bound from <c>"Authentication:Facebook"</c>. The verifier members configure the Limited
/// Login <c>id_token</c> verifier (audiences are the Facebook app ids); the <see cref="RedirectSignInOptions"/> ones
/// ASP.NET's Facebook redirect handler, with <c>ClientId</c> = the App ID and <c>ClientSecret</c> = the App Secret.
/// </summary>
public sealed class FacebookProviderOptions : ProviderVerifierOptions
{
    public FacebookProviderOptions()
    {
        MetadataAddress = "https://www.facebook.com/.well-known/openid-configuration/";
        Issuers = ["https://www.facebook.com", "https://facebook.com"];
    }
}

global using System.Runtime.CompilerServices;
global using System.Security.Claims;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Facebook;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Protocols;
global using Microsoft.IdentityModel.Protocols.OpenIdConnect;

global using Sencilla.Core;

using Sencilla.Authentication;

[assembly: InternalsVisibleTo("Sencilla.Authentication.Providers.Tests")]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    private const string Section = "Authentication:Facebook";

    /// <summary>
    /// Add Facebook as a social-login provider: registers the Limited Login <see cref="FacebookTokenVerifier"/> and
    /// binds <see cref="FacebookProviderOptions"/> from the <c>"Authentication:Facebook"</c> section, then
    /// <paramref name="configure"/>. With <see cref="RedirectSignInOptions.RedirectSignIn"/> it also registers
    /// ASP.NET's Facebook redirect handler (scheme <c>Facebook</c>, <c>/signin-facebook</c>), bound to the account
    /// graph by <see cref="AuthenticationConfig.AddRedirectSignIn"/>.
    /// </summary>
    public static AuthenticationConfig UseFacebook(this AuthenticationConfig config, Action<FacebookProviderOptions>? configure = null)
    {
        config.AddProvider<FacebookTokenVerifier, FacebookProviderOptions>("facebook", Section, configure);

        if (config.AddRedirectSignIn<FacebookProviderOptions, FacebookOptions>(
                "facebook", FacebookDefaults.AuthenticationScheme, Section, configure, FacebookRedirectSignIn.ReadIdentity) is { } facebook)
            config.Services.AddAuthentication().AddFacebook(o => FacebookRedirectSignIn.Configure(o, facebook));
        return config;
    }
}

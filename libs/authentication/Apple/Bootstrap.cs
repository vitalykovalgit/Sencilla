global using System.Runtime.CompilerServices;
global using System.Security.Claims;
global using System.Text.Json;

global using AspNet.Security.OAuth.Apple;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.OAuth;
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
    private const string Section = "Authentication:Apple";

    /// <summary>
    /// Add Apple (Sign in with Apple) as a social-login provider: registers the <see cref="AppleTokenVerifier"/> and
    /// binds <see cref="AppleProviderOptions"/> from the <c>"Authentication:Apple"</c> section, then
    /// <paramref name="configure"/>. With <see cref="RedirectSignInOptions.RedirectSignIn"/> it also registers the
    /// <c>AspNet.Security.OAuth.Apple</c> redirect handler (scheme <c>Apple</c>, <c>/signin-apple</c>, the answer
    /// posted back), bound to the account graph by <see cref="AuthenticationConfig.AddRedirectSignIn"/>.
    /// </summary>
    public static AuthenticationConfig UseApple(this AuthenticationConfig config, Action<AppleProviderOptions>? configure = null)
    {
        config.AddProvider<AppleTokenVerifier, AppleProviderOptions>("apple", Section, configure);

        if (config.AddRedirectSignIn<AppleProviderOptions, AppleAuthenticationOptions>(
                "apple", AppleAuthenticationDefaults.AuthenticationScheme, Section, configure, AppleRedirectSignIn.ReadIdentity) is { } apple)
            config.Services.AddAuthentication().AddApple(o => AppleRedirectSignIn.Configure(o, apple));
        return config;
    }
}

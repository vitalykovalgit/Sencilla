global using System.Runtime.CompilerServices;
global using System.Security.Claims;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Google;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Protocols;
global using Microsoft.IdentityModel.Protocols.OpenIdConnect;

global using Sencilla.Core;
global using Sencilla.Component.Users;

using Sencilla.Authentication;

[assembly: InternalsVisibleTo("Sencilla.Authentication.Providers.Tests")]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    private const string Section = "Authentication:Google";

    /// <summary>
    /// Add Google as a social-login provider: registers the <see cref="GoogleTokenVerifier"/> and binds
    /// <see cref="GoogleProviderOptions"/> from the <c>"Authentication:Google"</c> section, then
    /// <paramref name="configure"/>. With <see cref="RedirectSignInOptions.RedirectSignIn"/> it also
    /// registers ASP.NET's Google redirect handler (scheme <c>Google</c>), bound to the account graph by
    /// <see cref="AuthenticationConfig.AddRedirectSignIn"/> (the host's default schemes are left as they are).
    /// </summary>
    public static AuthenticationConfig UseGoogle(this AuthenticationConfig config, Action<GoogleProviderOptions>? configure = null)
    {
        config.AddProvider<GoogleTokenVerifier, GoogleProviderOptions>("google", Section, configure);

        if (config.AddRedirectSignIn<GoogleProviderOptions, GoogleOptions>(
                "google", GoogleDefaults.AuthenticationScheme, Section, configure, GoogleRedirectSignIn.ReadIdentity) is { } google)
            config.Services.AddAuthentication().AddGoogle(o => GoogleRedirectSignIn.Configure(o, google));
        return config;
    }
}

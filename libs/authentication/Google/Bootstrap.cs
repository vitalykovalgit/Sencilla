global using System.Runtime.CompilerServices;
global using System.Security.Claims;
global using System.Text.Json;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Google;
global using Microsoft.AspNetCore.Authentication.OAuth;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.Extensions.Configuration;
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
    /// <paramref name="configure"/>. With <see cref="GoogleProviderOptions.RedirectSignIn"/> it also
    /// registers ASP.NET's Google redirect handler, whose sign-ins resolve through
    /// <see cref="IExternalAuthService.ResolveAccount"/> (the host's default schemes are left as they are).
    /// </summary>
    public static AuthenticationConfig UseGoogle(this AuthenticationConfig config, Action<GoogleProviderOptions>? configure = null)
    {
        config.AddProvider<GoogleTokenVerifier, GoogleProviderOptions>("google", Section, configure);

        var google = new GoogleProviderOptions();
        config.Configuration.GetSection(Section).Bind(google);
        configure?.Invoke(google);
        if (!google.RedirectSignIn)
            return config;

        if (string.IsNullOrWhiteSpace(google.ClientId) || string.IsNullOrWhiteSpace(google.ClientSecret))
            throw new InvalidOperationException(
                "Google RedirectSignIn needs a ClientId and a ClientSecret (Authentication:Google or the UseGoogle lambda).");

        config.Services.AddAuthentication().AddGoogle(o => GoogleRedirectSignIn.Configure(o, google));
        return config;
    }
}

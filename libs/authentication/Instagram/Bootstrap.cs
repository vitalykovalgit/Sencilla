global using System.Runtime.CompilerServices;
global using System.Security.Claims;
global using System.Text.Encodings.Web;
global using System.Text.Json;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.OAuth;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

using Sencilla.Authentication;

[assembly: InternalsVisibleTo("Sencilla.Authentication.Providers.Tests")]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    private const string Section = "Authentication:Instagram";
    private const string Scheme = "Instagram";

    /// <summary>
    /// Add «Business Login for Instagram» as a redirect sign-in: binds <see cref="InstagramProviderOptions"/> from the
    /// <c>"Authentication:Instagram"</c> section, then <paramref name="configure"/>, and registers ASP.NET's generic OAuth
    /// handler as scheme <c>Instagram</c> on <c>/signin-instagram</c>, bound to the account graph by
    /// <see cref="AuthenticationConfig.AddRedirectSignIn"/>. Only professional (business or creator) accounts can sign in.
    /// </summary>
    public static AuthenticationConfig UseInstagram(this AuthenticationConfig config, Action<InstagramProviderOptions>? configure = null)
    {
        if (config.AddRedirectSignIn<InstagramProviderOptions, OAuthOptions>(
                "instagram", Scheme, Section, configure, InstagramRedirectSignIn.ReadIdentity) is { } instagram)
            config.Services.AddAuthentication()
                .AddOAuth<OAuthOptions, InstagramHandler>(Scheme, Scheme, o => InstagramRedirectSignIn.Configure(o, instagram));
        return config;
    }
}

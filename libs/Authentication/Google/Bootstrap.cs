global using System.Runtime.CompilerServices;

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
    /// <summary>
    /// Add Google as a social-login provider: registers the <see cref="GoogleTokenVerifier"/> and
    /// binds <see cref="GoogleProviderOptions"/> from the <c>"Authentication:Google"</c> section.
    /// </summary>
    public static AuthenticationConfig UseGoogle(this AuthenticationConfig config, Action<GoogleProviderOptions>? configure = null)
        => config.AddProvider<GoogleTokenVerifier, GoogleProviderOptions>("google", "Authentication:Google", configure);
}

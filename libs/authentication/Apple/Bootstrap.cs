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
    /// Add Apple (Sign in with Apple) as a social-login provider: registers the
    /// <see cref="AppleTokenVerifier"/> and binds <see cref="AppleProviderOptions"/> from the
    /// <c>"Authentication:Apple"</c> section.
    /// </summary>
    public static AuthenticationConfig UseApple(this AuthenticationConfig config, Action<AppleProviderOptions>? configure = null)
        => config.AddProvider<AppleTokenVerifier, AppleProviderOptions>("apple", "Authentication:Apple", configure);
}

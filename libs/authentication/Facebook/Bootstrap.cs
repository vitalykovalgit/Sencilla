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
    /// Add Facebook / Instagram (Limited Login) as a social-login provider: registers the
    /// <see cref="FacebookTokenVerifier"/> and binds <see cref="FacebookProviderOptions"/> from the
    /// <c>"Authentication:Facebook"</c> section.
    /// </summary>
    public static AuthenticationConfig UseFacebook(this AuthenticationConfig config, Action<FacebookProviderOptions>? configure = null)
        => config.AddProvider<FacebookTokenVerifier, FacebookProviderOptions>("facebook", "Authentication:Facebook", configure);
}

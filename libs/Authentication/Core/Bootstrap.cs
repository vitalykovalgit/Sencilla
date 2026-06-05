global using System.ComponentModel.DataAnnotations.Schema;
global using System.Runtime.CompilerServices;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Text;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Protocols;
global using Microsoft.IdentityModel.Protocols.OpenIdConnect;
global using Microsoft.IdentityModel.Tokens;

global using Sencilla.Core;
global using Sencilla.Component.Users;
global using Sencilla.Web.MinimalApi;

using Sencilla.Authentication;

[assembly: InternalsVisibleTo("Sencilla.Authentication.Providers.Tests")]
[assembly: InternalsVisibleTo("Sencilla.Authentication.Jwt.Tests")]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Single fluent entry point for the authentication family. Compose the app's authentication by
    /// plugging components (<c>UseJwtToken</c>, <c>UseAppAccounts</c>, <c>UseGoogle</c>, <c>UseUsers</c>),
    /// declaring what tokens it validates (<c>AcceptBearer</c>), and shaping the built-in endpoints
    /// (<c>UseApi</c>). Registration happens once, after the callback, in dependency order.
    /// </summary>
    public static IServiceCollection AddSencillaAuthentication(
        this IServiceCollection services, IConfiguration configuration, Action<AuthenticationConfig> configure)
    {
        var config = new AuthenticationConfig(services, configuration);
        configure(config);
        config.Build();
        return services;
    }

    /// <summary>
    /// Issue embedded HMAC access tokens and rotating refresh tokens. Identity binds from the
    /// <c>"Jwt"</c> section. Owns the <c>/refresh</c> and <c>/logout</c> endpoints.
    /// </summary>
    public static AuthenticationConfig UseJwtToken(this AuthenticationConfig config, Action<JwtTokenConfig>? configure = null)
    {
        var issuer = config.GetOrAddIssuer();
        configure?.Invoke(issuer);
        return config;
    }

    /// <summary>
    /// Enable email/password local accounts: register + login endpoints, password and lockout
    /// policy, optional phone login, and default roles for new users.
    /// </summary>
    public static AuthenticationConfig UseAppAccounts(this AuthenticationConfig config, Action<AppAccountsConfig>? configure = null)
    {
        var accounts = config.GetOrAddAppAccounts();
        configure?.Invoke(accounts);
        return config;
    }

    /// <summary>Select the <see cref="IUserStore"/> the family reads/writes users through.</summary>
    public static AuthenticationConfig UseUsers(this AuthenticationConfig config, Action<UserStoreConfig> configure)
    {
        var store = new UserStoreConfig();
        configure(store);
        if (store.Registration is not null)
            config.UserStore = store.Registration;
        return config;
    }

    /// <summary>
    /// Validate incoming bearer tokens. With no arguments it validates the embedded HMAC tokens this
    /// app issues (reusing the <c>UseJwtToken</c> key/issuer/audience); pass <c>b =&gt; b.Authority(...)</c>
    /// to validate an external IDP's tokens via its JWKS.
    /// </summary>
    public static AuthenticationConfig AcceptBearer(this AuthenticationConfig config, Action<BearerConfig>? configure = null)
    {
        var bearer = config.GetOrAddBearer();
        configure?.Invoke(bearer);
        return config;
    }

    /// <summary>Shape the built-in authentication endpoints (base path, opt-out, optional <c>/me</c>).</summary>
    public static AuthenticationConfig UseApi(this AuthenticationConfig config, Action<AuthApiConfig> configure)
    {
        configure(config.Api);
        return config;
    }
}

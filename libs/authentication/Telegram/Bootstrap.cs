global using System.Globalization;
global using System.Net;
global using System.Runtime.CompilerServices;
global using System.Security.Claims;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.OpenIdConnect;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using Sencilla.Authentication;

[assembly: InternalsVisibleTo("Sencilla.Authentication.Providers.Tests")]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    private const string Section = "Authentication:Telegram";
    private const string Scheme = "Telegram";

    /// <summary>
    /// Add Log In With Telegram as a redirect sign-in: binds <see cref="TelegramProviderOptions"/> from the
    /// <c>"Authentication:Telegram"</c> section, then <paramref name="configure"/>, and registers ASP.NET's OpenID
    /// Connect handler as scheme <c>Telegram</c> on <c>/signin-telegram</c>, bound to the account graph by
    /// <see cref="AuthenticationConfig.AddRedirectSignIn"/>. A shared phone becomes a confirmed phone of a new account;
    /// the bot's permission to write rides the identity as <see cref="TelegramRawClaims.BotChatId"/>.
    /// </summary>
    public static AuthenticationConfig UseTelegram(this AuthenticationConfig config, Action<TelegramProviderOptions>? configure = null)
    {
        if (config.AddRedirectSignIn<TelegramProviderOptions, OpenIdConnectOptions>(
                "telegram", Scheme, Section, configure, TelegramRedirectSignIn.ReadIdentity) is { } telegram)
            config.Services.AddAuthentication().AddOpenIdConnect(Scheme, Scheme, o => TelegramRedirectSignIn.Configure(o, telegram));
        return config;
    }
}

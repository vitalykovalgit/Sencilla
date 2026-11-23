namespace Sencilla.Authentication;

/// <summary>
/// Telegram configuration, bound from <c>"Authentication:Telegram"</c>: the bot's Login (OpenID Connect) client from
/// @BotFather — <c>ClientId</c> (the bot id) and <c>ClientSecret</c>. A redirect sign-in is Telegram's only shape, so
/// <see cref="RedirectSignInOptions.RedirectSignIn"/> starts on.
/// </summary>
public sealed class TelegramProviderOptions : RedirectSignInOptions
{
    public TelegramProviderOptions() => RedirectSignIn = true;
}

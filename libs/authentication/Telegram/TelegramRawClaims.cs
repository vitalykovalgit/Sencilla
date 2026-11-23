namespace Sencilla.Authentication;

/// <summary>The <see cref="ExternalIdentity.RawClaims"/> keys a Telegram identity carries.</summary>
public static class TelegramRawClaims
{
    /// <summary>
    /// The chat our bot may write to — for the private chat of a sign-in, the user's Telegram id. Present only when the
    /// user let the bot write to them; absent when they refused. A host reads it off the identity of
    /// <see cref="LoginSucceeded.ExternalIdentity"/> to connect its bot notifications.
    /// </summary>
    public const string BotChatId = "bot_chat_id";
}

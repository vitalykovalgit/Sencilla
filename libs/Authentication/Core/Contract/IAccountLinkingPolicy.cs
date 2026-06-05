namespace Sencilla.Authentication;

/// <summary>
/// Pure, side-effect-free decision for an incoming external identity. Identity is keyed on the
/// provider subject (never email), and email collisions never silently merge — that is the
/// account-takeover defense. The default implementation is registered when any social provider
/// (<c>UseGoogle</c>/<c>UseFacebook</c>/<c>UseApple</c>) is configured.
/// </summary>
public interface IAccountLinkingPolicy
{
    /// <param name="byProvider">Existing account already linked to this provider + subject, if any.</param>
    /// <param name="byEmail">Existing account matching the incoming email, if the email is usable.</param>
    /// <param name="incoming">The verified external identity.</param>
    /// <param name="authenticatedUserId">
    /// The signed-in user's id when this is an explicit link flow, otherwise <c>null</c>.
    /// </param>
    AccountDecision Decide(AuthUser? byProvider, AuthUser? byEmail, ExternalIdentity incoming, Guid? authenticatedUserId);
}

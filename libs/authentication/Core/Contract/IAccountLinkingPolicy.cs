namespace Sencilla.Authentication;

/// <summary>
/// Pure, side-effect-free decision for an incoming external identity — the one rule table behind both
/// the <c>/external</c> token exchange and a host-side redirect sign-in. Identity is keyed on the
/// provider subject; an address only matters when no account has that subject yet: a provider-verified
/// address links to the account that owns it (displacing an unverified password credential there), and
/// an unverified address never does. The default implementation is registered when any social provider
/// (<c>UseGoogle</c>/<c>UseFacebook</c>/<c>UseApple</c>) is configured.
/// </summary>
public interface IAccountLinkingPolicy
{
    /// <param name="byProvider">Existing account already linked to this provider + subject, if any.</param>
    /// <param name="byEmail">
    /// Existing account that owns the incoming email address, if any — looked up whether or not the
    /// provider verified the address; the policy decides what an unverified one means.
    /// </param>
    /// <param name="incoming">The verified external identity.</param>
    /// <param name="authenticatedUserId">
    /// The signed-in user's id when this is an explicit link flow, otherwise <c>null</c>.
    /// </param>
    AccountDecision Decide(AuthUser? byProvider, AuthUser? byEmail, ExternalIdentity incoming, Guid? authenticatedUserId);
}

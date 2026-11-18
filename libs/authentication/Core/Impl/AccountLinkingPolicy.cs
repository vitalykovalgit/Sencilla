namespace Sencilla.Authentication;

/// <summary>
/// Default account-linking policy — pure and side-effect-free, so it is exhaustively unit-tested.
/// Identity is keyed on the provider subject. A provider-verified address links to the account that
/// owns it (the provider proved the caller controls it, and an unverified password credential there is
/// displaced); an unverified address on somebody's account is refused, because anyone can put any
/// address on a social profile.
/// </summary>
public sealed class AccountLinkingPolicy : IAccountLinkingPolicy
{
    public AccountDecision Decide(AuthUser? byProvider, AuthUser? byEmail, ExternalIdentity incoming, Guid? authenticatedUserId)
    {
        // 1) This provider identity is already linked to an account.
        if (byProvider is not null)
        {
            // A signed-in user whose session belongs to a different account must not steal it.
            if (authenticatedUserId is { } uid && byProvider.Id != uid)
                return AccountDecision.Reject("provider-linked-to-another-account");

            return IsUsable(byProvider.Status)
                ? AccountDecision.Link("provider-known")
                : AccountDecision.Reject("account-not-active");
        }

        // 2) A signed-in user is attaching a new provider to their own account.
        if (authenticatedUserId is not null)
            return AccountDecision.Link("authenticated-link");

        // 3) Anonymous caller, first time with this provider identity, and the address is an account's.
        //    Only the provider's word that the caller owns the address may attach the login to it.
        if (byEmail is not null)
        {
            if (!IsUsable(byEmail.Status))
                return AccountDecision.Reject("account-not-active");

            return incoming.EmailVerified
                ? AccountDecision.LinkByEmail("provider-verified-email")
                : AccountDecision.Reject("email-not-verified");
        }

        // 4) Nobody has this address — provision a new account from the external identity.
        return AccountDecision.Create("new-account");
    }

    private static bool IsUsable(AuthUserStatus status)
        => status is AuthUserStatus.Active or AuthUserStatus.PendingVerification;
}

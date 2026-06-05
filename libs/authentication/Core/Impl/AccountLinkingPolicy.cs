namespace Sencilla.Authentication;

/// <summary>
/// Default account-linking policy — pure and side-effect-free, so it is exhaustively unit-tested.
/// Identity is keyed on the provider subject; a same-email account never silently merges, which is
/// the pre-account-takeover defense.
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

        // 3) Anonymous caller, first time with this provider identity. Only treat a same-email
        //    account as a collision when the provider asserts a verified email; otherwise the
        //    email is untrusted and identity is keyed on the subject.
        if (incoming.EmailVerified && byEmail is not null)
        {
            return IsUsable(byEmail.Status)
                ? AccountDecision.Challenge("email-collision-requires-proof")
                : AccountDecision.Reject("account-not-active");
        }

        // 4) No collision — provision a new account from the external identity.
        return AccountDecision.Create("new-account");
    }

    private static bool IsUsable(AuthUserStatus status)
        => status is AuthUserStatus.Active or AuthUserStatus.PendingVerification;
}

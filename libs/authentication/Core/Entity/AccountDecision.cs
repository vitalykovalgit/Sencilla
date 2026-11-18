namespace Sencilla.Authentication;

/// <summary>
/// What to do with an incoming external identity relative to the existing account graph.
/// </summary>
public enum LinkAction
{
    /// <summary>No matching account — provision a new user from the external identity.</summary>
    Create,

    /// <summary>The provider identity is already this account's (or the signed-in user is attaching it to their own).</summary>
    Link,

    /// <summary>
    /// The provider verified the address, so the login is attached to the account that owns it; an
    /// unverified password credential on that account is displaced, not merged with.
    /// </summary>
    LinkByEmail,

    /// <summary>Refuse (e.g. account disabled, or an unverified provider address that belongs to an account).</summary>
    Reject,
}

/// <summary>
/// Pure decision produced by <see cref="IAccountLinkingPolicy"/>. A provider-verified address links to
/// the account that owns it; an unverified address never does. The <see cref="Reason"/> is carried for
/// telemetry and, on <see cref="LinkAction.Reject"/>, is the error code the caller receives.
/// </summary>
public sealed record AccountDecision(LinkAction Action, string? Reason = null)
{
    public static AccountDecision Create(string? reason = null) => new(LinkAction.Create, reason);
    public static AccountDecision Link(string? reason = null) => new(LinkAction.Link, reason);
    public static AccountDecision LinkByEmail(string? reason = null) => new(LinkAction.LinkByEmail, reason);
    public static AccountDecision Reject(string? reason = null) => new(LinkAction.Reject, reason);
}

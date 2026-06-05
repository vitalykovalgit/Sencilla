namespace Sencilla.Authentication;

/// <summary>
/// What to do with an incoming external identity relative to the existing account graph.
/// </summary>
public enum LinkAction
{
    /// <summary>No matching account — provision a new user from the external identity.</summary>
    Create,

    /// <summary>Safe to attach the external login to an existing, already-proven account.</summary>
    Link,

    /// <summary>A collision exists (e.g. same email) — require proof of control before linking.</summary>
    Challenge,

    /// <summary>Refuse (e.g. account disabled, or an unsafe silent-link attempt).</summary>
    Reject,
}

/// <summary>
/// Pure decision produced by <see cref="IAccountLinkingPolicy"/>. The <see cref="Reason"/> is
/// carried for telemetry and to drive the challenge experience.
/// </summary>
public sealed record AccountDecision(LinkAction Action, string? Reason = null)
{
    public static AccountDecision Create(string? reason = null) => new(LinkAction.Create, reason);
    public static AccountDecision Link(string? reason = null) => new(LinkAction.Link, reason);
    public static AccountDecision Challenge(string? reason = null) => new(LinkAction.Challenge, reason);
    public static AccountDecision Reject(string? reason = null) => new(LinkAction.Reject, reason);
}

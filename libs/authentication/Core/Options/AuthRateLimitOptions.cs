namespace Sencilla.Authentication;

/// <summary>A sliding window: at most <paramref name="Permits"/> calls per caller within <paramref name="Period"/>.</summary>
public sealed record RateLimitWindow(int Permits, TimeSpan Period);

/// <summary>
/// The limits the built-in sign-in and recovery endpoints put on themselves (<see cref="AuthRateLimiter"/>).
/// A refused call answers <c>429 { "error": "rate-limited" }</c>. The client address is
/// <c>HttpContext.Connection.RemoteIpAddress</c>, so a host behind a proxy must apply its forwarded
/// headers first. Switched off with <c>UseApi(o =&gt; o.RateLimits.Enabled = false)</c>.
/// </summary>
public sealed class AuthRateLimitOptions
{
    /// <summary>When false, no endpoint is limited.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary><c>/login</c>, keyed by client address + the trimmed, lower-cased login: password guessing on one account.</summary>
    public RateLimitWindow LoginPerAddressAndEmail { get; set; } = new(10, TimeSpan.FromMinutes(10));

    /// <summary><c>/login</c>, keyed by client address: one address spraying many accounts.</summary>
    public RateLimitWindow LoginPerAddress { get; set; } = new(50, TimeSpan.FromMinutes(10));

    /// <summary><c>/register</c>, keyed by client address.</summary>
    public RateLimitWindow RegisterPerAddress { get; set; } = new(5, TimeSpan.FromHours(1));

    /// <summary>
    /// <c>/forgot-password</c> keyed by the trimmed, lower-cased email, and <c>/resend-verification</c> keyed
    /// by the signed-in user's id: how often one inbox can be mailed, from any address.
    /// </summary>
    public RateLimitWindow MailPerRecipient { get; set; } = new(3, TimeSpan.FromMinutes(15));

    /// <summary><c>/forgot-password</c> and <c>/resend-verification</c>, keyed by client address: one address mailing many inboxes.</summary>
    public RateLimitWindow MailPerAddress { get; set; } = new(10, TimeSpan.FromHours(1));

    /// <summary><c>/reset-password</c>, <c>/verify-email</c> and <c>/change-password</c>, keyed by client address: token and password guessing.</summary>
    public RateLimitWindow TokenPerAddress { get; set; } = new(10, TimeSpan.FromHours(1));

    /// <summary><c>/external</c>, keyed by client address.</summary>
    public RateLimitWindow ExternalPerAddress { get; set; } = new(10, TimeSpan.FromMinutes(10));
}

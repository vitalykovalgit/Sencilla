using System.Threading.RateLimiting;

namespace Sencilla.Authentication;

/// <summary>
/// The sign-in and recovery endpoints' own guard against guessing and mail floods: one sliding window
/// per <see cref="AuthRateLimitOptions"/> property, counted separately for each caller of it. Each
/// <c>Allows*</c> spends a permit from every window it checks and answers whether the call may proceed;
/// all answer true while <see cref="AuthRateLimitOptions.Enabled"/> is false.
/// </summary>
// ponytail: windows are counted per process, so each replica counts separately; move to a shared store if replicas make that matter.
public sealed class AuthRateLimiter(IOptions<AuthApiOptions> api) : IDisposable
{
    // Every caller without a remote address (no forwarded headers applied, an in-process test) shares this bucket.
    private const string UnknownAddress = "unknown";

    // A partition is named after its window as well as its caller, so two windows with equal numbers never share a bucket.
    private readonly PartitionedRateLimiter<(string Partition, RateLimitWindow Window)> _windows =
        PartitionedRateLimiter.Create<(string Partition, RateLimitWindow Window), string>(call =>
            RateLimitPartition.GetSlidingWindowLimiter(call.Partition, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = call.Window.Permits,
                Window = call.Window.Period,
                SegmentsPerWindow = 4,
                QueueLimit = 0,
            }));

    private AuthRateLimitOptions Limits => api.Value.RateLimits;

    /// <summary><see cref="AuthRateLimitOptions.LoginPerAddress"/> and <see cref="AuthRateLimitOptions.LoginPerAddressAndEmail"/>.</summary>
    public bool AllowsLogin(HttpContext http, string login)
        => Allows(nameof(AuthRateLimitOptions.LoginPerAddress), Limits.LoginPerAddress, AddressOf(http))
        && Allows(nameof(AuthRateLimitOptions.LoginPerAddressAndEmail), Limits.LoginPerAddressAndEmail, $"{AddressOf(http)}|{Normalized(login)}");

    /// <summary><see cref="AuthRateLimitOptions.RegisterPerAddress"/>.</summary>
    public bool AllowsRegistration(HttpContext http)
        => Allows(nameof(AuthRateLimitOptions.RegisterPerAddress), Limits.RegisterPerAddress, AddressOf(http));

    /// <summary>
    /// <see cref="AuthRateLimitOptions.MailPerAddress"/> and <see cref="AuthRateLimitOptions.MailPerRecipient"/>;
    /// <paramref name="recipient"/> is the email asked about, or the signed-in user's id.
    /// </summary>
    public bool AllowsMail(HttpContext http, string recipient)
        => Allows(nameof(AuthRateLimitOptions.MailPerAddress), Limits.MailPerAddress, AddressOf(http))
        && Allows(nameof(AuthRateLimitOptions.MailPerRecipient), Limits.MailPerRecipient, Normalized(recipient));

    /// <summary><see cref="AuthRateLimitOptions.TokenPerAddress"/>.</summary>
    public bool AllowsTokenUse(HttpContext http)
        => Allows(nameof(AuthRateLimitOptions.TokenPerAddress), Limits.TokenPerAddress, AddressOf(http));

    /// <summary><see cref="AuthRateLimitOptions.ExternalPerAddress"/>.</summary>
    public bool AllowsExternalSignIn(HttpContext http)
        => Allows(nameof(AuthRateLimitOptions.ExternalPerAddress), Limits.ExternalPerAddress, AddressOf(http));

    public void Dispose() => _windows.Dispose();

    private bool Allows(string windowName, RateLimitWindow window, string caller)
    {
        if (!Limits.Enabled)
            return true;

        using var lease = _windows.AttemptAcquire(($"{windowName}|{caller}", window));
        return lease.IsAcquired;
    }

    private static string AddressOf(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? UnknownAddress;

    // The body is untrusted: a JSON null still reaches here despite the non-nullable property.
    private static string Normalized(string? value) => value?.Trim().ToLowerInvariant() ?? "";
}

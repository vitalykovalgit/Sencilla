namespace Sencilla.Authentication;

/// <summary>Options of the cookie session mode, set by <c>UseCookieSession(o =&gt; ...)</c>.</summary>
public sealed class CookieSessionOptions
{
    /// <summary>The host's cookie authentication scheme the endpoints sign into. Default <c>Cookies</c>.</summary>
    public string Scheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>
    /// How often a cookie principal is re-checked against the user store (disabled account, password
    /// changed after the cookie was issued). One store read per session per interval.
    /// </summary>
    public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromMinutes(5);
}

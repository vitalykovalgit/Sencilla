namespace Sencilla.Component.Security;

/// <summary>
/// Host configuration for admin impersonation. Registered as a singleton by
/// <c>services.AddSencillaImpersonation(o =&gt; ...)</c>.
///
/// The cookie attributes deliberately mirror the host's auth cookie rather than defaulting on their
/// own: the impersonation cookie must travel on exactly the requests the session cookie travels on,
/// or the SPA and the API would disagree about who the caller is on some paths and not others.
/// </summary>
public class ImpersonationOptions
{
    /// <summary>
    /// Kill switch. False makes <see cref="ImpersonationMiddleware"/> a pass-through and the endpoints
    /// 404 — the feature can be turned off in a running environment without a client redeploy.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cookie carrying the impersonated user's id. HttpOnly; never read by client script.</summary>
    public string CookieName { get; set; } = "impersonate";

    /// <summary>Parent domain (e.g. ".example.com") so the cookie is first-party across SPA and API hosts. Unset locally.</summary>
    public string? CookieDomain { get; set; }

    /// <summary>SameSite=None + Secure, for a SPA on a different host to the API. Mirrors the auth cookie.</summary>
    public bool CrossSite { get; set; }

    /// <summary>
    /// Absolute lifetime of an impersonation session. Bounded on purpose: the realistic accident is an
    /// admin walking away mid-impersonation and later acting as the customer.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The matrix resource that gates the capability. An app-level id (the host owns its resource
    /// namespace), re-checked on EVERY request — the cookie is not signed, and a grant may be revoked
    /// mid-session.
    /// </summary>
    public string Resource { get; set; } = "admin.users.impersonate";

    /// <summary>Action demanded on <see cref="Resource"/>.</summary>
    public Action Action { get; set; } = Action.Update;

    /// <summary>
    /// Where the browser-navigable escape route sends the operator after clearing the cookie. Must be an
    /// absolute URL when the SPA is on a different host to the API — the escape route exists for sessions
    /// where the app itself is too broken to offer a button, so it cannot land back on the API host.
    /// </summary>
    public string ExitRedirectUrl { get; set; } = "/";
}

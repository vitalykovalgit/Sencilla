using Microsoft.AspNetCore.Http;

namespace Sencilla.Component.Security;

/// <summary>
/// The one place that knows the impersonation cookie's shape. The middleware reads it and the host's
/// endpoints write it, and a Set/Delete pair whose attributes differ by so much as a Domain silently
/// fails to clear — so name, domain, path and SameSite are defined here once rather than at each call site.
///
/// The value is <c>{targetId}|{expiresAtUnixSeconds}</c> rather than a bare id for two reasons: the
/// browser's own cookie expiry is advisory (a client may simply not honour it), and the UI has to be able
/// to read the deadline back to warn before the session ends.
///
/// It is deliberately NOT signed. Forging one buys nothing: <see cref="ImpersonationMiddleware"/>
/// re-checks the grant on every request, so a caller who could mint an accepted cookie is a caller who
/// could have asked for it through the endpoint anyway.
/// </summary>
public class ImpersonationCookie(ImpersonationOptions options)
{
    /// <summary>The live impersonation, or null when the cookie is absent, malformed or expired.</summary>
    public (Guid TargetId, DateTimeOffset ExpiresAt)? Read(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(options.CookieName, out var raw) || string.IsNullOrEmpty(raw))
            return null;

        var parts = raw.Split('|');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var targetId) || !long.TryParse(parts[1], out var unix))
            return null;

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(unix);

        // Enforced here as well as by the cookie's own Expires: the lifetime is a safety bound on an
        // unattended session, and a bound only the client honours is not a bound.
        return expiresAt <= DateTimeOffset.UtcNow ? null : (targetId, expiresAt);
    }

    /// <summary>Starts (or restarts) the lifetime window. Returns the absolute deadline, for the caller to report.</summary>
    public DateTimeOffset Set(HttpContext context, Guid targetId)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(options.Lifetime);
        context.Response.Cookies.Append(options.CookieName, $"{targetId}|{expiresAt.ToUnixTimeSeconds()}", Build(expiresAt));

        return expiresAt;
    }

    /// <summary>
    /// Clears the cookie. Also called by the middleware whenever a cookie fails validation, so a
    /// revoked, stale or expired impersonation cannot keep costing a permission lookup on every request.
    /// </summary>
    public void Clear(HttpContext context)
    {
        // Delete() only matches on domain/path/secure, so it must be built from the same options as Set().
        if (!context.Response.HasStarted)
            context.Response.Cookies.Delete(options.CookieName, Build(null));
    }

    CookieOptions Build(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        // SameSite=None requires Secure; both are mandatory for cross-site XHR to the API host.
        Secure = options.CrossSite,
        SameSite = options.CrossSite ? SameSiteMode.None : SameSiteMode.Lax,
        Domain = string.IsNullOrEmpty(options.CookieDomain) ? null : options.CookieDomain,
        Path = "/",
        Expires = expires,
    };
}

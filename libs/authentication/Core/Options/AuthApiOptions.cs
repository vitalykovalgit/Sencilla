namespace Sencilla.Authentication;

/// <summary>
/// Runtime options the built-in authentication endpoints read (base path, opt-out, optional
/// <c>/me</c>, rate limits). Set by <c>UseApi(o =&gt; ...)</c> and resolved by the endpoint classes via
/// <see cref="IOptions{T}"/>.
/// </summary>
public sealed class AuthApiOptions
{
    /// <summary>Base path the anonymous auth routes mount under. Default <c>/api/v1/auth</c>.</summary>
    public string BasePath { get; set; } = "/api/v1/auth";

    /// <summary>When true, the library maps no endpoints — the host wires its own.</summary>
    public bool Disabled { get; set; }

    /// <summary>When true (and a bearer scheme or a cookie session is configured), map the protected <c>GET</c> me endpoint.</summary>
    public bool MeEnabled { get; set; }

    /// <summary>Path of the protected current-user endpoint. Default <c>/api/v1/me</c>.</summary>
    public string MePath { get; set; } = "/api/v1/me";

    /// <summary>How often the sign-in and recovery endpoints answer one caller; on by default.</summary>
    public AuthRateLimitOptions RateLimits { get; } = new();
}

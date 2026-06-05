namespace Sencilla.Authentication;

/// <summary>
/// Runtime options the built-in authentication endpoints read (base path, opt-out, optional
/// <c>/me</c>). Populated by <c>UseApi(...)</c> and resolved by the endpoint classes via
/// <see cref="IOptions{T}"/>.
/// </summary>
public sealed class AuthApiOptions
{
    /// <summary>Base path the anonymous auth routes mount under. Default <c>/api/v1/auth</c>.</summary>
    public string BasePath { get; set; } = "/api/v1/auth";

    /// <summary>When true, the library maps no endpoints — the host wires its own.</summary>
    public bool Disabled { get; set; }

    /// <summary>When true (and a bearer scheme is configured), map the protected <c>GET</c> me endpoint.</summary>
    public bool MeEnabled { get; set; }

    /// <summary>Path of the protected current-user endpoint. Default <c>/api/v1/me</c>.</summary>
    public string MePath { get; set; } = "/api/v1/me";
}

/// <summary>Fluent configuration for the built-in authentication endpoints (<c>UseApi</c>).</summary>
public sealed class AuthApiConfig
{
    internal AuthApiOptions Options { get; } = new();

    /// <summary>Base path the anonymous auth routes mount under (alias of <see cref="BasePath"/>).</summary>
    public AuthApiConfig Url(string basePath)
    {
        Options.BasePath = basePath;
        return this;
    }

    public AuthApiConfig BasePath(string basePath)
    {
        Options.BasePath = basePath;
        return this;
    }

    /// <summary>Do not map any endpoints — the host wires its own.</summary>
    public AuthApiConfig Disable()
    {
        Options.Disabled = true;
        return this;
    }

    /// <summary>Map the protected current-user endpoint (off by default).</summary>
    public AuthApiConfig MapMe(bool on = true)
    {
        Options.MeEnabled = on;
        return this;
    }

    public AuthApiConfig MePath(string path)
    {
        Options.MePath = path;
        return this;
    }
}

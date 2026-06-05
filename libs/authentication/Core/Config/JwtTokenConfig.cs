namespace Sencilla.Authentication;

/// <summary>Where rotating refresh tokens are persisted.</summary>
public enum RefreshStorage
{
    /// <summary>Process-local store — single-instance / dev use.</summary>
    InMemory,

    /// <summary>SQL-backed store over <c>sec.RefreshToken</c> (survives restarts, multi-instance).</summary>
    EntityFramework,
}

/// <summary>
/// Configures the embedded HMAC token issuer (<c>UseJwtToken</c>). Issuer identity (signing key,
/// issuer, audience, access-token lifetime) binds from the <c>"Jwt"</c> configuration section by
/// default; <see cref="Configure"/> overrides it in code.
/// </summary>
public sealed class JwtTokenConfig
{
    internal Action<JwtIssuerOptions>? OverrideOptions { get; private set; }
    internal RefreshTokenConfig RefreshConfig { get; } = new();

    /// <summary>Override the issuer options resolved from the <c>"Jwt"</c> section.</summary>
    public JwtTokenConfig Configure(Action<JwtIssuerOptions> configure)
    {
        OverrideOptions = configure;
        return this;
    }

    /// <summary>Configure the rotating refresh-token policy and its storage.</summary>
    public JwtTokenConfig RefreshToken(Action<RefreshTokenConfig> configure)
    {
        configure(RefreshConfig);
        return this;
    }
}

/// <summary>Rotating refresh-token policy and storage selection (under <c>UseJwtToken</c>).</summary>
public sealed class RefreshTokenConfig
{
    internal int? LifetimeDaysValue { get; private set; }
    internal bool? RotateValue { get; private set; }
    internal bool? DetectReuseValue { get; private set; }
    internal RefreshStorage Storage { get; private set; } = RefreshStorage.InMemory;

    /// <summary>Refresh-token lifetime (rounded up to whole days).</summary>
    public RefreshTokenConfig Lifetime(TimeSpan ttl)
    {
        LifetimeDaysValue = (int)Math.Ceiling(ttl.TotalDays);
        return this;
    }

    public RefreshTokenConfig LifetimeDays(int days)
    {
        LifetimeDaysValue = days;
        return this;
    }

    /// <summary>Re-issue a fresh token on every redeem (sliding session).</summary>
    public RefreshTokenConfig Rotate(bool on = true)
    {
        RotateValue = on;
        return this;
    }

    /// <summary>Revoke the whole token family when a redeemed token is replayed.</summary>
    public RefreshTokenConfig DetectReuse(bool on = true)
    {
        DetectReuseValue = on;
        return this;
    }

    public RefreshTokenConfig PersistInMemory()
    {
        Storage = RefreshStorage.InMemory;
        return this;
    }

    public RefreshTokenConfig PersistEntityFramework()
    {
        Storage = RefreshStorage.EntityFramework;
        return this;
    }
}

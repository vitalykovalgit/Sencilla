namespace Sencilla.Authentication;

/// <summary>Issuer / audience / access-token lifetime for issued tokens.</summary>
public sealed class AuthOptions
{
    public string Issuer { get; set; } = default!;

    public IList<string> Audiences { get; set; } = new List<string>();

    public int AccessTokenLifetimeMinutes { get; set; } = 10;
}

/// <summary>Refresh-token lifetime, rotation, and reuse-detection policy.</summary>
public sealed class RefreshTokenOptions
{
    public int LifetimeDays { get; set; } = 14;

    public bool Rotate { get; set; } = true;

    public bool DetectReuse { get; set; } = true;
}

/// <summary>Password policy + Argon2id cost parameters + optional server-side pepper.</summary>
public sealed class PasswordPolicyOptions
{
    public int MinLength { get; set; } = 12;

    public bool CheckBreached { get; set; } = true;

    /// <summary>Argon2id memory cost in KiB (default 19 MiB, the OWASP minimum).</summary>
    public int Argon2MemoryKb { get; set; } = 19_456;

    public int Argon2Iterations { get; set; } = 3;

    public int Argon2Parallelism { get; set; } = 1;

    /// <summary>
    /// Optional secret mixed into every hash. Stored with the signing secrets (k8s / Vault),
    /// never in the database, so a database leak alone cannot crack hashes.
    /// </summary>
    public string? Pepper { get; set; }
}

/// <summary>Brute-force lockout thresholds.</summary>
public sealed class LockoutOptions
{
    public int MaxFailedAttempts { get; set; } = 5;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan BaseLockout { get; set; } = TimeSpan.FromMinutes(1);
}

namespace Sencilla.Authentication;

/// <summary>Input for password registration. <see cref="Phone"/> is optional profile data.</summary>
public sealed record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public long? Phone { get; init; }
}

/// <summary>
/// Input for password login. <see cref="Login"/> is an email address or a phone number; phone
/// resolution only happens when the app opted in via <c>UseAppAccounts(o =&gt; o.AllowPhoneLogin())</c>.
/// </summary>
public sealed record LoginRequest
{
    public required string Login { get; init; }
    public required string Password { get; init; }
}

/// <summary>Input for a social token-exchange: a provider <c>id_token</c> and its provider key.</summary>
public sealed record ExternalLoginRequest
{
    public required string Provider { get; init; }
    public required string IdToken { get; init; }
}

/// <summary>Input for a refresh-token exchange (and logout, which revokes the token's family).</summary>
public sealed record RefreshRequest
{
    public required string RefreshToken { get; init; }
}

/// <summary>Issued token pair for the token-API / embedded path.</summary>
public sealed record TokenResponse
{
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
}

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
/// resolution only happens when the app opted in via <c>UseAppAccounts(o =&gt; o.PhoneLoginEnabled = true)</c>.
/// <see cref="RememberMe"/> asks for a persistent session; only the cookie session transport honours it.
/// </summary>
public sealed record LoginRequest
{
    public required string Login { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; }
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

/// <summary>Input for the forgot-password flow — always returns 200 to prevent enumeration.</summary>
public sealed record ForgotPasswordRequest
{
    public required string Email { get; init; }
}

/// <summary>Input for the reset-password flow — the raw token from the emailed link.</summary>
public sealed record ResetPasswordRequest
{
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}

/// <summary>Input for the verify-email flow — the raw token from the emailed link.</summary>
public sealed record VerifyEmailRequest
{
    public required string Token { get; init; }
}

/// <summary>
/// Deliberately empty bodies. A JSON body parameter makes the endpoint reject anything that is not
/// <c>application/json</c> (415), which is what keeps a cross-site HTML form post — which rides the
/// cookie — away from state-changing endpoints that otherwise need no input.
/// </summary>
public sealed record LogoutRequest;

public sealed record ResendVerificationRequest;

/// <summary>Input for an authenticated password change.</summary>
public sealed record ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

/// <summary>
/// How the session an <see cref="ISessionTransport"/> starts should behave. The cookie transport honours
/// <see cref="Persistent"/>; the token transport ignores it (the refresh policy sets the pair's lifetime).
/// </summary>
public sealed record SessionLifetime(bool Persistent = false);

/// <summary>Issued token pair for the token-API / embedded path.</summary>
public sealed record TokenResponse
{
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
}

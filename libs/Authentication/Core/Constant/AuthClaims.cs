namespace Sencilla.Authentication;

/// <summary>
/// Canonical claim names written into issued tokens. The legacy <c>Sencilla.Component.Users</c>
/// <c>ClaimType</c> constants fold in here during cutover.
/// </summary>
public static class AuthClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string Name = "name";
    public const string Picture = "picture";
    public const string Roles = "roles";

    /// <summary>Authentication methods reference (e.g. <c>pwd</c>, <c>otp</c>).</summary>
    public const string Amr = "amr";

    /// <summary>Authentication context class reference (assurance level).</summary>
    public const string Acr = "acr";
}

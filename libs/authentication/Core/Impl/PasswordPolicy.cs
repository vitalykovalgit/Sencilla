namespace Sencilla.Authentication;

/// <summary>Server-side enforcement of <see cref="PasswordPolicyOptions"/>; the one place every password enters.</summary>
public static class PasswordPolicy
{
    /// <summary>
    /// Throws <see cref="BadRequestException"/> <c>password-too-short</c> below the minimum length and
    /// <c>password-policy</c> for every other rule (length cap, digit, mixed case).
    /// </summary>
    public static void Validate(string? password, PasswordPolicyOptions options)
    {
        if (password is null || password.Length < options.MinLength)
            throw new BadRequestException("password-too-short");
        if (password.Length > options.MaxLength)
            throw new BadRequestException("password-policy");
        if (options.RequireDigit && !password.Any(char.IsDigit))
            throw new BadRequestException("password-policy");
        if (options.RequireMixedCase && !(password.Any(char.IsUpper) && password.Any(char.IsLower)))
            throw new BadRequestException("password-policy");
    }
}

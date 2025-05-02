

namespace System;

public static class PasswordHashExtensions
{
    private static readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

    public static string HashPassword(this string password)
    {
        return _hasher.HashPassword(null, password);
    }
    public static bool VerifyPassword(this string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success;
    }
}

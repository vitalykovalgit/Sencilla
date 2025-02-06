
using Microsoft.AspNetCore.Identity;

namespace Sencilla.Component.Users.Auth;

public static class PasswordHashHelper
{
    private static readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

    public static string HashPassword(string password)
    {
        return _hasher.HashPassword(null, password);
    }
    public static bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success;
    }
}

namespace Sencilla.Authentication;

/// <summary>
/// Projects a principal for the endpoints, accepting both claim vocabularies: the family's
/// (<c>sub</c>, <c>email</c>, <c>email_verified</c>, <c>roles</c>) and the <c>System.Security.Claims</c>
/// names a bearer handler maps them to.
/// </summary>
internal static class SessionUser
{
    public static MeResponse Project(ClaimsPrincipal user)
    {
        var id = user.FindFirst(AuthClaims.Subject)?.Value
              ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(AuthClaims.Email)?.Value
                 ?? user.FindFirst(ClaimTypes.Email)?.Value;
        var emailVerified = string.Equals(user.FindFirst(AuthClaims.EmailVerified)?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var name = user.FindFirst(AuthClaims.Name)?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value;
        var picture = user.FindFirst(AuthClaims.Picture)?.Value;
        var roles = user.FindAll(AuthClaims.Roles).Select(c => c.Value)
            .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct()
            .ToArray();

        return new MeResponse(id, email, emailVerified, name, picture, roles);
    }
}

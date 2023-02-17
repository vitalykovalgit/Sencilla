
namespace Sencilla.Component.Users;

static class ClaimsPrincipalEx 
{
    // 
    public static string? GetClaimValue(this ClaimsPrincipal principal, string type)
    {
        return principal.Claims.FirstOrDefault(c => c.Type == type)?.Value;
    }

    public static IEnumerable<string> GetClaimValues(this ClaimsPrincipal principal, string type)
    {
        return principal.Claims.Where(c => c.Type == type).Select( c => c.Value );
    }

    public static User ToUser(this ClaimsPrincipal principal)
    {
        // try parse phone 
        long.TryParse(principal?.GetClaimValue(ClaimTypes.MobilePhone), out long phone);

        return new User
        {
            Phone = phone,
            Email = principal?.GetClaimValue(ClaimTypes.Email),
            FirstName = principal?.GetClaimValue(ClaimTypes.GivenName),
            LastName = principal?.GetClaimValue(ClaimTypes.Surname),
        };
    }
}

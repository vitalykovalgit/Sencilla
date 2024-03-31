
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

        var user = new User
        {
            Phone = phone,
            Email = principal?.GetClaimValue(ClaimTypes.Email),
            EmailConf = principal?.GetClaimValue(ClaimType.EmailConfirmed) != null,
            FirstName = principal?.GetClaimValue(ClaimTypes.GivenName),
            LastName = principal?.GetClaimValue(ClaimTypes.Surname),
            Pic = principal?.GetClaimValue(ClaimType.Picture),
            Status = (byte?)UserStatuses.NotRegistered,
        };

        if (Enum.TryParse(principal?.GetClaimValue(ClaimType.RegStatus), true, out UserStatuses regStatus))
            user.Status = (byte?)regStatus;

        return user;
    }
}

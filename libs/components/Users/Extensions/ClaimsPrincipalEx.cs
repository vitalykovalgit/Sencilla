namespace Sencilla.Component.Users;

public static class ClaimsPrincipalEx
{
    /// <summary>
    /// The account id the session carries: the <see cref="ClaimType.UserId"/> claim, else
    /// <see cref="ClaimTypes.NameIdentifier"/> (where a JWT bearer handler maps <c>sub</c>), when it parses
    /// as a Guid; otherwise null.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirst(ClaimType.UserId)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var userId) ? userId : null;
    }
}

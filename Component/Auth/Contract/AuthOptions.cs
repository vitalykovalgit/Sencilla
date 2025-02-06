namespace Sencilla.Component.Users.Auth;

public class AuthOptions
{
    public string SecretKey { get; set; } = default!;

    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    public int JwtExpiresMinutes { get; set; } = 60;
}

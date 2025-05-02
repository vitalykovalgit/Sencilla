namespace Sencilla.Component.Users.Auth;

public class TokenResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}

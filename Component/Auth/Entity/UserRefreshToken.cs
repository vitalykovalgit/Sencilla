namespace Sencilla.Component.Users.Auth;

[Table(nameof(UserRefreshToken), Schema = "sec")]
public class UserRefreshToken : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
}

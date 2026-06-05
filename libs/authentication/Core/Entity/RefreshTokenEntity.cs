namespace Sencilla.Authentication;

[Table("RefreshToken", Schema = "sec")]
public class RefreshTokenEntity : IEntity<string>
{
    public string Id { get; set; } = default!;
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public bool Revoked { get; set; }
}

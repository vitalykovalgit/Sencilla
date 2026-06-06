namespace Sencilla.Authentication;

[Table("PasswordResetToken", Schema = "sec")]
public class PasswordResetToken : IEntity<Guid>, IEntityCreateable, IEntityUpdateable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

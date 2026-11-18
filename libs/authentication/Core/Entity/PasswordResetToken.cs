namespace Sencilla.Authentication;

/// <summary>What an emailed single-use token is for. Stored on the row so a reset link can never confirm an address and vice versa.</summary>
public static class TokenPurpose
{
    public const string Reset = "reset";
    public const string Verify = "verify";
}

[Table("PasswordResetToken", Schema = "sec")]
public class PasswordResetToken : IEntity<Guid>, IEntityCreateable, IEntityUpdateable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string Purpose { get; set; } = TokenPurpose.Reset;
}

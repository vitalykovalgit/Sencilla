namespace Sencilla.Component.Users;

/// <summary>
/// 
/// </summary>
[Table(nameof(UserRole), Schema = "sec")]
public class UserRole 
    : IEntity
    //, IEntityCreateableTrack
    //, IEntityUpdateableTrack
    , IEntityDeleteable
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Column("Role")]
    public int RoleId { get; set; }

    [NotMapped]
    public string? Role { get; set; }

    //public DateTime CreatedDate { get; set; }
    //public DateTime UpdatedDate { get; set; }
}

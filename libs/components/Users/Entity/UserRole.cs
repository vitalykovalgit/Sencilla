namespace Sencilla.Component.Users;

/// <summary>
/// 
/// </summary>
[CrudApi("api/v1/users/roles")]
[Table(nameof(UserRole), Schema = "sec")]

public class UserRole 
    : IEntity
    , IEntityCreateable
    , IEntityDeleteable
    , IEntityUpdateable
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Column("Role")]
    public int RoleId { get; set; }

    [NotMapped]
    public string? Role => Enum.GetName(typeof(RoleType), RoleId);  

    //public DateTime CreatedDate { get; set; }
    //public DateTime UpdatedDate { get; set; }
}

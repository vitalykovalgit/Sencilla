namespace Sencilla.Component.Users;

[Table(nameof(UserRole), Schema = "sec")]
[CrudApi("api/v1/users/roles")]
public class UserRole : IEntity<int>
    , IEntityCreateable
    , IEntityUpdateable
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

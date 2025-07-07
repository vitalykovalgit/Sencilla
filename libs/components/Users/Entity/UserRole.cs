using System.ComponentModel.DataAnnotations;
using Sencilla.Component.Security;

namespace Sencilla.Component.Users;

public enum UserRoleType
{
    Root = 1,
    Anonymous = 2,
    User = 3,
    Guest = 4,
    Admin = 5
}

[Table(nameof(UserRole), Schema = "sec")]
[CrudApi("api/v1/users/roles")]
public class UserRole : IEntity<int>
    , IEntityCreateable
    , IEntityUpdateable
    , IEntityDeleteable
{
    public int Id { get; set; }

    [Column("Role")]
    public int RoleId { get; set; }
    public int UserId { get; set; }           

    public ICollection<Matrix> Matrix { get; set; }

    [NotMapped]
    public string? Role => ((UserRoleType)RoleId).ToString();
}

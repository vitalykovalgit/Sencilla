
namespace Sencilla.Component.Users;

[MainEntity(typeof(User))]
[CrudApi("api/v1/users/names")]
[Table(nameof(User), Schema = "sec")]
public class UserName: IEntity, IEntityUpdateable
{
    public int Id { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
}

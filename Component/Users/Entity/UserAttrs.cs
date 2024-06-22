
namespace Sencilla.Component.Users;

[MainEntity(typeof(User))]
[CrudApi("api/v1/users/attrs")]
[Table(nameof(User), Schema = "sec")]
public class UserAttrs: IEntity, IEntityUpdateable
{
    public int Id { get; set; }

    public string? Attrs { get; set; }
}

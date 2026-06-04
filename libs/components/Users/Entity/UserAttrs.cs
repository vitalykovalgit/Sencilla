
namespace Sencilla.Component.Users;

[MainEntity(typeof(User))]
[CrudApi("api/v1/users/attrs")]
[Table(nameof(User), Schema = "sec")]
public class UserAttrs: IEntity<Guid>, IEntityUpdateable
{
    public Guid Id { get; set; }

    [JsonObjectString]
    public string? Attrs { get; set; }
}


namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/contacts")]
[Table(nameof(UserContact), Schema = "sec")]
public class UserContact: IEntity<Guid>, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public byte Type { get; set; }
    public required string Contact { get; set; }
    public byte Order { get; set; }

    public DateTime CreatedDate { get; set; }
}

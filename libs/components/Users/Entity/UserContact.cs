
namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/contacts")]
[Table(nameof(UserContact), Schema = "sec")]
public class UserContact: IEntity<byte>, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public byte Id { get; set; }
    public int UserId { get; set; }
    
    public byte Type { get; set; }
    public required string Phone { get; set; }
    public byte Order { get; set; }

    public DateTime CreatedDate { get; set; }
}

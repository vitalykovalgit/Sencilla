
namespace Sencilla.Component.Users;

[MainEntity(typeof(User))]
[CrudApi("api/v1/users/phone")]
[Table(nameof(User), Schema = "sec")]
public class UserPhone: IEntity, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }
    public long Phone { get; set; }
    public bool PhoneConf { get; set; }
}

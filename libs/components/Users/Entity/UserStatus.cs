namespace Sencilla.Component.Users;

[UseCaching(60)]
[CrudApi("api/v1/users/statuses")]
[Table(nameof(UserStatus), Schema = "sec")]
public class UserStatus: IEntity<byte>
{
    public byte Id { get; set; }
    public required string Name { get; set; }
}

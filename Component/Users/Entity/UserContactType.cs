namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/contacttypes")]
[Table(nameof(UserContactType), Schema = "sec")]
public class UserContactType: IEntity<byte>
{
    public byte Id { get; set; }
    public required string Name { get; set; }
}

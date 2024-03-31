namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/types")]
[Table(nameof(UserType), Schema = "sec")]
public class UserType: IEntity<byte>
{
    public byte Id { get; set; }
    public required string Name { get; set; }
}

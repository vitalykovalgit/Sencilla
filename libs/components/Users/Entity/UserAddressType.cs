namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/addresstypes")]
[Table(nameof(UserAddressType), Schema = "sec")]
public class UserAddressType: IEntity<byte>
{
    public byte Id { get; set; }
    public required string Name { get; set; }
}

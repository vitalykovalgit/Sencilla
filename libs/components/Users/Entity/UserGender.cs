namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/genders")]
[Table(nameof(UserGender), Schema = "sec")]
public class UserGender: IEntity<byte>
{
    public byte Id { get; set; }
    public required string Name { get; set; }
}

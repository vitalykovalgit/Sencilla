namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/contacttypes")]
[Table(nameof(UserContactType), Schema = "sec")]
public class UserContactType: IEntity<byte>, IEntityOrderable<byte>, IEntityHideable 
{
    public byte Id { get; set; }
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public bool Hidden { get; set; }
    public byte Order { get; set; }
}

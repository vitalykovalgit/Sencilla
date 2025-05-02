namespace Sencilla.Component.Users;

[MainEntity(typeof(User))]
[Table(nameof(User), Schema = "sec")]
public class UserPassword: IEntity, IEntityUpdateable
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = default!;

}

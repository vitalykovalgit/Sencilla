namespace Sencilla.Component.Users;

[MainEntity(typeof(User))]
[Table(nameof(User), Schema = "sec")]
public class UserPassword: IEntity<Guid>, IEntityUpdateable
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = default!;

}

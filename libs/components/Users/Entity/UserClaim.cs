
namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/claims")]
[Table(nameof(UserClaim), Schema = "sec")]
public class UserClaim: IEntity<Guid>, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string? Name { get; set; }
    public string? Value { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

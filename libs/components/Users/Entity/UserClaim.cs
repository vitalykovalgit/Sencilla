
namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/claims")]
[Table(nameof(UserClaim), Schema = "sec")]
public class UserClaim: IEntity, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    public string? Name { get; set; }
    public string? Value { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}


namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/auths")]
[Table(nameof(UserAuth), Schema = "sec")]
public class UserAuth: IEntity<Guid>, IEntityCreateableTrack, IEntityUpdateable, IEntityDeleteable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Auth { get; set; }
    public required string Email { get; set; }

    public DateTime CreatedDate { get; set; }
}

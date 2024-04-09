
namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/auths")]
[Table(nameof(UserAuth), Schema = "sec")]
public class UserAuth: IEntity<byte>, IEntityCreateableTrack, IEntityUpdateable, IEntityDeleteable
{
    public byte Id { get; set; }
    public int UserId { get; set; }
    
    public required string Auth { get; set; }
    public required string Email { get; set; }

    public DateTime CreatedDate { get; set; }
}


namespace Sencilla.Component.Users;

/// <summary>
/// 
/// </summary>
public class UserRole 
    : IEntity
    , IEntityCreateableTrack
    , IEntityUpdateableTrack
    , IEntityDeleteable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string? Role { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

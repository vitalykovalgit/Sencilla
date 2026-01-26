using Microsoft.EntityFrameworkCore.Extension.Entity.Attributes;

namespace Sencilla.Component.Users;

[Table(nameof(User), Schema = "sec")]
public class User: IEntity, IEntityCreateableTrack, IEntityUpdateableTrack, IEntityRemoveable, IEntityDeleteable
{
    public int Id { get; set; }

    public long Phone { get; set; }
    public bool PhoneConf { get; set; }

    public string? Email { get; set; }
    public bool EmailConf { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
    public string? Pic { get; set; }

    public byte? Gender { get; set; }
    public byte? Status { get; set; }
    public byte? Type { get; set; }

    public string? Comments { get; set; }

    [JsonObjectString]
    public string? Attrs { get; set; }

    public DateTime? BirthDate { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    [SkipUpsert]
    public ICollection<UserRole>? Roles { get; set; }

    [NotMapped]
    public ICollection<UserClaim>? Claims { get; set; }

    public User AddRole(int role) 
    {
        var roles = Roles ??= new List<UserRole>();
        roles.Add(new UserRole
        {
            UserId = Id,
            RoleId = role
            //Role = role.ToString(),
        });

        return this;
    }

    public bool IsAnonymous() 
    {
        return string.IsNullOrEmpty(Email) && Phone == 0;
    }
}

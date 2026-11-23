using Sencilla.Extensions.EntityFrameworkCore.Entity.Attributes;

namespace Sencilla.Component.Users;

[Table(nameof(User), Schema = "sec")]
public class User : IEntity<Guid>, IEntityCreateableTrack, IEntityUpdateableTrack, IEntityRemoveable
{
    public Guid Id { get; set; }

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

    /// <summary>Last password set / reset / removal. A session issued before it is rejected by the authentication family's cookie validator.</summary>
    public DateTime? CredentialsChangedAt { get; set; }

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

    /// <summary>
    /// The visitor: a caller with no account. The session carries an account id, so the id decides — not a
    /// contact: an account with neither an email nor a phone (a social sign-in that gave no contact) is still
    /// an account, with an account's rights.
    /// </summary>
    public bool IsAnonymous() => Id == Guid.Empty;

    /// <summary>The user a request runs as when its session carries no account: no id, the Anonymous role.</summary>
    public static User CreateAnonymous()
    {
        return new User { Status = (byte?)UserStatuses.NotRegistered }.AddRole((int)RoleType.Anonymous);
    }
}


namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/auths")]
[Table(nameof(UserAuth), Schema = "sec")]
public class UserAuth: IEntity<Guid>, IEntityCreateableTrack, IEntityUpdateable, IEntityDeleteable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Provider key: <c>password</c>, <c>google</c>, <c>apple</c>, <c>facebook</c>, ...</summary>
    public string Auth { get; set; } = default!;

    /// <summary>Provider subject for social logins, or the normalized email for the password provider.</summary>
    public string? ProviderKey { get; set; }

    /// <summary>Informational email reported by the provider (may be a relay or absent).</summary>
    public string? Email { get; set; }

    /// <summary>Password hash for the <c>password</c> provider row; null for external logins.</summary>
    public string? PasswordHash { get; set; }

    public DateTime CreatedDate { get; set; }
}


namespace Sencilla.Component.Users;

[CrudApi("api/v1/users/addresses")]
[Table(nameof(UserAddress), Schema = "sec")]
public class UserAddress: IEntity, IEntityCreateableTrack, IEntityUpdateableTrack, IEntityDeleteable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public byte Type { get; set; }

    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? RegionId { get; set; }
    public int? DistrictId { get; set; }
    public int? CityId { get; set; }

    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? House { get; set; }
    public string? Apart { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

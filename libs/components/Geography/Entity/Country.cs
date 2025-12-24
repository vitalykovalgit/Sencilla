namespace Sencilla.Component.Geography;

[CrudApi("api/v1/countries")]
[Table(nameof(Country), Schema = "geo")]
public class Country : IEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Iso2 { get; set; }
    public required string Iso3 { get; set; }
    public required string Code { get; set; }
    public required string PhoneCode { get; set; }
    public required string UsaName { get; set; }
}

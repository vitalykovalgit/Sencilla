namespace Sencilla.Component.Geography;

[CrudApi("api/v1/languages")]
[Table(nameof(Language), Schema = "geo")]
public class Language : IEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
}


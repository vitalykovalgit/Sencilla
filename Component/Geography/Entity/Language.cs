namespace Sencilla.Component.Geography;

[CrudApi("api/v1/languages")]
public class Language : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }
}


namespace Sencilla.Component.Geography;

[CrudApi("api/v1/countries/languages")]
public class CountryLanguage : IEntity
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public int LanguageId { get; set; }
}


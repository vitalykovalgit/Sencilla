﻿namespace Sencilla.Component.Geography;

[CrudApi("api/v1/countries/languages")]
[Table(nameof(CountryLanguage), Schema = "geo")]
public class CountryLanguage : IEntity
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public int LanguageId { get; set; }
}


namespace Sencilla.Component.I18n;

[CrudApi("api/v1/i18n/resource")]
public class ResourceView : IEntity<string>
{
    [Key]
    public string Id { get; set; } = default!;

    public string Description { get; set; } = default!;

    public string TranslationString { get; set; } = default!;

    [InverseProperty(nameof(Translation.ResourceView))]
    public ICollection<Translation>? Translations { get; set; }

    //public string[] SearchBy => [nameof(Id), nameof(Description), nameof(TranslationString)];
}

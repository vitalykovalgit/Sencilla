namespace Sencilla.Component.I18n;

public class ResourceView : IEntity<string>
{
    [Key]
    public string Id { get; set; }

    public string Description { get; set; }

    public string TranslationString { get; set; }

    [InverseProperty(nameof(Translation.ResourceView))]
    public ICollection<Translation>? Translations { get; set; }

    public string[] SearchBy => [nameof(Id), nameof(Description), nameof(TranslationString)];
}

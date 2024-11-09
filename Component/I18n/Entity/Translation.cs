namespace Sencilla.Component.I18n;

public class Translation : IEntity, IEntityCreateable
{
    public int Id { get; set; }

    public string ResourceId { get; set; }

    public int LanguageId { get; set; }

    public string Value { get; set; }

    [NotMapped]
    public DateTime CreatedDate { get; set; }

    [NotMapped]
    public DateTime UpdatedDate { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public Resource Resource { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public ResourceView ResourceView { get; set; }
}

public class TranslationFilter : Filter<Translation>
{
    public TranslationFilter ByResourceId(params string[] resourceId)
    {
        AddProperty(nameof(Translation.ResourceId), typeof(string), resourceId.Cast<object>().ToArray());
        return this;
    }

    public TranslationFilter ByLanguageId(int languageId)
    {
        AddProperty(nameof(Translation.LanguageId), typeof(int), languageId);
        return this;
    }
}

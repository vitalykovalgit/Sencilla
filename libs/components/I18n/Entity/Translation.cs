namespace Sencilla.Component.I18n;

/// <summary>
/// 
/// </summary>
[CrudApi("api/v1/i18n/translations")]
public class Translation : IEntity, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }

    public string ResourceId { get; set; } = default!;

    public int LanguageId { get; set; }

    public string Value { get; set; } = default!;


    [ForeignKey(nameof(ResourceId))]
    public Resource? Resource { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public ResourceView? ResourceView { get; set; }
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

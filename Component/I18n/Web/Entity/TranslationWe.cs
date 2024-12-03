namespace Sencilla.Component.I18n;

public class TranslationWe : IDtoEntity<Translation>
{
    public int Id { get; set; }

    public string ResourceId { get; set; }

    public int LanguageId { get; set; }

    public string Value { get; set; }

    public TranslationWe() { }

    public TranslationWe(Translation entity) => FromEntity(entity);

    public Translation ToEntity(Translation entity)
    {
        entity.Id = Id;
        entity.ResourceId = ResourceId;
        entity.LanguageId = LanguageId;
        entity.Value = Value;

        return entity;
    }

    public void FromEntity(Translation entity)
    {
        Id = entity.Id;
        ResourceId = entity.ResourceId;
        LanguageId = entity.LanguageId;
        Value = entity.Value;
    }
}

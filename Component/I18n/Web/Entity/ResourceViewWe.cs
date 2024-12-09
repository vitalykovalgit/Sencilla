namespace Sencilla.Component.I18n;

public class ResourceViewWe : IDtoEntity<ResourceView, string>
{
    public string Id { get; set; }

    public string Description { get; set; }

    public ICollection<TranslationWe>? Translations { get; set; }

    public ResourceViewWe() { }

    public ResourceViewWe(ResourceView entity) => FromEntity(entity);

    public void FromEntity(ResourceView entity)
    {
        Id = entity.Id;
        Description = entity.Description;
        Translations = entity.Translations?.Select(x => new TranslationWe(x))?.ToList();
    }

    public ResourceView ToEntity(ResourceView entity)
    {
        entity.Id = Id;
        entity.Description = Description;
        entity.Translations = Translations?.Select(x => x.ToEntity(new ()))?.ToList();

        return entity;
    }
}

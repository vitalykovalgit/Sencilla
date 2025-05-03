namespace Sencilla.Component.I18n;

/// <summary>
/// Resource entity
/// </summary>
[CrudApi("api/v1/i18n/resources")]
public class Resource 
        : IEntity<string>
        , IEntityCreateableTrack
        , IEntityUpdateableTrack
        , IEntityDeleteable
{
    public string Id { get; set; } = default!;

    public string Description { get; set; } = default!;

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    [InverseProperty(nameof(Translation.Resource))]
    public ICollection<Translation>? Translations { get; set; }

    //public string[] SearchBy => [nameof(Id), nameof(Description)];
}

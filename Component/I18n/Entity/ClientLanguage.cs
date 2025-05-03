namespace Sencilla.Component.I18n;

[CrudApi("api/v1/i18n/languages/clients")]
public class ClientLanguage : IEntity, IEntityUpdateable, IEntityCreateable, IEntityDeleteable
{
    public int Id { get; set; }

    public int LanguageId { get; set; }

    public bool Hidden { get; set; }

    //[NotMapped]
    //public DateTime CreatedDate { get; set; }
    //[NotMapped]
    //public DateTime UpdatedDate { get; set; }

    [ForeignKey(nameof(LanguageId))]
    public Language? Language { get; set; }
}

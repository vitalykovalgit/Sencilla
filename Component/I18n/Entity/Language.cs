namespace Sencilla.Component.I18n;

public class Language : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string NativeName { get; set; }

    public string Locale { get; set; }

    public int Order { set; get; }

    [NotMapped]
    public DateTime CreatedDate { get; set; }

    [NotMapped]
    public DateTime UpdatedDate { get; set; }
}

namespace Sencilla.Component.I18n;

public class LanguageWe
{
    public LanguageWe(Language clientLanguage)
    {
        //FromEntity(clientLanguage);
    }

    public int Id { get; set; }

    public string Name { get; set; }

    public string Locale { get; set; }

    public string NativeName { get; set; }

    public int Order { set; get; }
}

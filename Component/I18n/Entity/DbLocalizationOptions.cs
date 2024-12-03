namespace Sencilla.Component.I18n;

public class DbLocalizationOptions
{
    public string DefaultLocale { get; set; }

    public IList<Type> Providers { get; set; } = new List<Type>();
}

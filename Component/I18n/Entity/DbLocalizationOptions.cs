namespace Sencilla.Component.I18n;

public class DbLocalizationOptions
{
    public string DefaultLocale { get; set; } = default!;

    public IList<Type>? Providers { get; set; }
}

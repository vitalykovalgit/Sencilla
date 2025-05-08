namespace Sencilla.Component.I18n;

public class DbStringLocalizer : IStringLocalizer
{
    private readonly ILocalizationProvider _localizationProvider;

    public DbStringLocalizer(ILocalizationProvider localizationProvider)
    {
        _localizationProvider = localizationProvider;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = _localizationProvider.GetString(name, CultureInfo.CurrentUICulture.Name).Result;
            return new LocalizedString(name, value ?? name, value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = _localizationProvider.GetString(name, CultureInfo.CurrentUICulture.Name).Result;

            var textDefinition = new TranslateDefinition { Text = format };
            var transform = new NumericTranslateTextTransform();
            transform.Transform(textDefinition);

            format = textDefinition.Text?.Replace("{{", "{").Replace("}}", "}");
            var value = string.Format(CultureInfo.CurrentCulture, format ?? name, arguments);

            return new LocalizedString(name, value ?? name, value == null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var values = _localizationProvider.GetStrings(CultureInfo.CurrentUICulture.Name).Result;
        return values.Select(value => new LocalizedString(value.Key, value.Value ?? value.Key, value.Value == null));
    }
}

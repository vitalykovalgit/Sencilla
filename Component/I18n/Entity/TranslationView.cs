namespace Sencilla.Component.I18n;

public class TranslationView : IEntity
{
    [Key]
    public int Id { get; set; }

    public string Key { get; set; }

    public string Locale { get; set; }

    public string Value { get; set; }
}

public class TranslationViewFilter : Filter<TranslationView>
{
    public string KeyStartWith { get; private set; }

    public TranslationViewFilter ByLocale(string locale)
    {
        AddProperty(nameof(TranslationView.Locale), typeof(string), locale);
        return this;
    }

    public TranslationViewFilter ByKey(string key)
    {
        AddProperty(nameof(TranslationView.Key), typeof(string), key);
        return this;
    }

    public TranslationViewFilter ByKeyStartsWith(string value)
    {
        KeyStartWith = value;
        return this;
    }
}

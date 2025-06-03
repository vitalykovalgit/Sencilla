namespace Sencilla.Component.I18n;

public class TranslateSettings
{
    public string ProviderName { get; set; } = default!;

    public bool OnlyEmpty { get; set; }

    public int[]? LanguageIds { get; set; }
}

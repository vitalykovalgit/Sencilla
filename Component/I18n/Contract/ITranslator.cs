namespace Sencilla.Component.I18n;

public interface ITranslator
{
    string Name { get; }
    bool Default { get; }
    Task<string[]> TranslateText(string[] values, string from, string to);
    Task<string[]> GetSupportedLanguages();
}

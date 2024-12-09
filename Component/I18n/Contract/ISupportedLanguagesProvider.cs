namespace Sencilla.Component.I18n;

public interface ISupportedLanguagesProvider
{
    Task<IList<Language>> GetSupportedLanguages();
}

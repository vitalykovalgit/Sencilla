namespace Sencilla.Component.I18n;

public interface ITranslateService
{
    Task TranslateLanguage(int languageId, TranslateSettings translateSettings);
}

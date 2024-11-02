namespace Sencilla.Component.I18n;

public class CacheSupportedLanguageProvider: ISupportedLanguagesProvider
{
    private readonly ISupportedLanguagesProvider provider;

    public CacheSupportedLanguageProvider(ISupportedLanguagesProvider provider)
    {
        this.provider = provider;
    }

    public Task<IList<Language>> GetSupportedLanguages()
    {
        throw new NotImplementedException();
    }
}

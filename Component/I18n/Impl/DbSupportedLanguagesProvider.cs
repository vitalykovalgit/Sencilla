namespace Sencilla.Component.I18n;

public class DbSupportedLanguagesProvider : ISupportedLanguagesProvider
{
    private readonly IReadRepository<ClientLanguage> _clientLanguageReadRepo;

    public DbSupportedLanguagesProvider(IReadRepository<ClientLanguage> clientLanguageReadRepo)
    {
        _clientLanguageReadRepo = clientLanguageReadRepo;
    }

    public async Task<IList<Language>> GetSupportedLanguages()
    {
        var languages = (await _clientLanguageReadRepo.GetAll(with: with => with.Language))
            .Where(p => !p.Hidden)
            .Select(p => p.Language)
            .ToList();

        return languages;
    }
}

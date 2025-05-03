namespace Sencilla.Component.I18n;

public class DbLocalizationProvider : ILocalizationProvider
{
    private readonly IReadRepository<TranslationView> _readRepository;

    public DbLocalizationProvider(IReadRepository<TranslationView> readRepository, IOptions<DbLocalizationOptions> options)
    {
        _readRepository = readRepository;
    }

    public async Task<string?> GetString(string resourceKey, string locale) =>
        (await _readRepository.GetAll(new TranslationViewFilter().ByLocale(locale).ByKey(resourceKey))).FirstOrDefault()?.Value;

    public async Task<Dictionary<string, string>> GetStrings(string locale) =>
        (await _readRepository.GetAll(new TranslationViewFilter().ByLocale(locale))).ToDictionary(t => t.Key, t => t.Value);

    public async Task<Dictionary<string, string>> GetStringsByGroup(string group, string locale) =>
        (await _readRepository.GetAll(new TranslationViewFilter().ByLocale(locale).ByKeyStartsWith(group))).ToDictionary(t => t.Key, t => t.Value);
}

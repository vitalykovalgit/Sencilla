namespace Sencilla.Component.I18n;

/// <summary>
/// Composes several localization providers; later-added providers win (list is walked in reverse).
/// Not a DI service: AddI18nServices builds it manually with an explicit provider list —
/// [DisableInjection] keeps auto-discovery from registering it (IList is not DI-resolvable,
/// and auto-binding it as ILocalizationProvider would make the aggregator wrap itself).
/// </summary>
[DisableInjection]
public class LocalizationProviderAggregator : ILocalizationProvider
{
    private readonly IList<ILocalizationProvider> _providers;

    public LocalizationProviderAggregator(IList<ILocalizationProvider> providers)
    {
        _providers = providers;
    }

    public async Task<string?> GetString(string resourceKey, string locale)
    {
        foreach (var provider in _providers.Reverse())
        {
            string? value = await provider.GetString(resourceKey, locale);
            if (value != null)
                return value;
        }

        return null;
    }

    public Task<Dictionary<string, string>> GetStrings(string locale) => GetStringsByLocale(locale);

    public async Task<Dictionary<string, string>> GetStringsByLocale(string locale)
    {
        var translationDict = new Dictionary<string, string>();
        foreach (var provider in _providers.Reverse())
        {
            var dict = await provider.GetStrings(locale);
            translationDict.MergeInPlace(dict);
        }

        return translationDict;
    }

    public async Task<Dictionary<string, string>> GetStringsByGroup(string ns, string locale)
    {
        var target = new Dictionary<string, string>();
        foreach (var provider in _providers.Reverse())
        {
            var source = await provider.GetStringsByGroup(ns, locale);
            target.MergeInPlace(source);
        }

        return target;
    }
}

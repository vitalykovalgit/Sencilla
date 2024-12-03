namespace Sencilla.Component.I18n;

public class LocalizationProviderAggregator : ILocalizationProvider
{
    private readonly IList<ILocalizationProvider> _providers;

    public LocalizationProviderAggregator(IList<ILocalizationProvider> providers)
    {
        _providers = providers;
    }

    public async Task<string> GetString(string resourceKey, string locale)
    {
        foreach (var provider in _providers.Reverse())
        {
            string value = await provider.GetString(resourceKey, locale);

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

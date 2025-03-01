namespace Sencilla.Component.I18n;

public class CacheLocalizationProvider: ILocalizationProvider
{
    private const char _separator = '.';

    private readonly ILocalizationProvider _provider;
    private readonly int _cacheLevel;

    private readonly Dictionary<string, Dictionary<string, string>> _caches = new ();

    public CacheLocalizationProvider(ILocalizationProvider provider, int cacheLevel = 1)
    {
        _provider = provider;
        _cacheLevel = cacheLevel;
    }

    public async Task<string> GetString(string resourceKey, string locale)
    {
        if (resourceKey == null)
            return null;

        var cache = GetCacheByLocale(locale);

        if (!cache.TryGetValue(resourceKey, out string value))
        {
            var groupkey = ResolveResourceGroup(resourceKey);

            var batch = await _provider.GetStringsByGroup(groupkey, locale);

            cache.MergeInPlace(batch);

            cache.TryGetValue(resourceKey, out value);

            if (value == null)
                cache.TryAdd(resourceKey, value);
        }

        return value;
    }

    public Task<Dictionary<string, string>> GetStrings(string locale) => _provider.GetStrings(locale);

    public Task<Dictionary<string, string>> GetStringsByGroup(string ns, string locale) => _provider.GetStringsByGroup(ns, locale);

    private string ResolveResourceGroup(string key)
    {
        string[] parts = key.Split(_separator);

        int groupLength = Math.Max(parts.Length - _cacheLevel, 1);

        string group = string.Join(_separator, parts.Take(groupLength).ToArray());

        return group;
    }

    private Dictionary<string, string> GetCacheByLocale(string locale)
    {
        if (!_caches.ContainsKey(locale))
            _caches[locale] = new Dictionary<string, string>();

        return _caches[locale];
    }
}

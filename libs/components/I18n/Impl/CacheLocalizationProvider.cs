using System.Collections.Concurrent;

namespace Sencilla.Component.I18n;

/// <summary>
/// Caches resolved strings in front of another provider, per locale, with a time bound.
///
/// The bound is what makes an admin edit visible without a restart. An explicit invalidation hook
/// exists too (<see cref="Invalidate"/>, fired by <see cref="TranslationCacheInvalidator"/>), but it
/// can only ever clear the process that served the write — behind a load balancer every OTHER
/// instance still holds the old value, and only expiry frees it. So the TTL is the guarantee and the
/// hook is the fast path.
///
/// Entries are also cached NEGATIVELY: a key with no translation is remembered as absent, or every
/// render of an untranslated key would re-query the database for a row that is not there.
/// </summary>
public class CacheLocalizationProvider : ILocalizationProvider
{
    private const char Separator = '.';

    /// <summary>How long a cached group may lag the database. Short enough that an editor sees their own change.</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(60);

    private readonly ILocalizationProvider _provider;
    private readonly int _cacheLevel;
    private readonly TimeSpan _ttl;
    private readonly TimeProvider _clock;

    private readonly ConcurrentDictionary<string, LocaleCache> _caches = new();

    public CacheLocalizationProvider(ILocalizationProvider provider, int cacheLevel = 1, TimeSpan? ttl = null, TimeProvider? clock = null)
    {
        _provider = provider;
        _cacheLevel = cacheLevel;
        _ttl = ttl ?? DefaultTtl;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>Drops everything cached. Called when a translation is written in THIS process.</summary>
    public void Invalidate() => _caches.Clear();

    public async Task<string?> GetString(string resourceKey, string locale)
    {
        if (resourceKey == null)
            return null;

        var cache = GetCacheByLocale(locale);
        var group = ResolveResourceGroup(resourceKey);

        if (!cache.TryGet(group, resourceKey, _clock.GetUtcNow(), out var value))
        {
            var batch = await _provider.GetStringsByGroup(group, locale);
            cache.Fill(group, batch, _clock.GetUtcNow().Add(_ttl));
            cache.TryGet(group, resourceKey, _clock.GetUtcNow(), out value);
        }

        return value;
    }

    // Whole-locale reads are the JSON endpoint's path: it is served to a browser that caches it
    // itself, and passing it through a per-group cache would return a half-filled catalog.
    public Task<Dictionary<string, string>> GetStrings(string locale) => _provider.GetStrings(locale);

    public Task<Dictionary<string, string>> GetStringsByGroup(string ns, string locale) => _provider.GetStringsByGroup(ns, locale);

    private string ResolveResourceGroup(string key)
    {
        var parts = key.Split(Separator);
        var groupLength = Math.Max(parts.Length - _cacheLevel, 1);

        return string.Join(Separator, parts.Take(groupLength).ToArray());
    }

    private LocaleCache GetCacheByLocale(string locale) => _caches.GetOrAdd(locale, _ => new LocaleCache());

    /// <summary>One locale's groups, each with its own expiry.</summary>
    private sealed class LocaleCache
    {
        private readonly ConcurrentDictionary<string, Group> _groups = new();

        public bool TryGet(string group, string key, DateTimeOffset now, out string? value)
        {
            value = null;

            if (!_groups.TryGetValue(group, out var entry) || entry.ExpiresAt <= now)
                return false;

            return entry.Values.TryGetValue(key, out value);
        }

        public void Fill(string group, Dictionary<string, string> values, DateTimeOffset expiresAt) =>
            _groups[group] = new Group(values, expiresAt);

        private sealed record Group(Dictionary<string, string> Values, DateTimeOffset ExpiresAt);
    }
}

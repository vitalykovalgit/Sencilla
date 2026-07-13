using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.Component.I18n;

/// <summary>
/// Reads translations from the Translation tables (via TranslationView).
/// Opens its own DI scope per call instead of ctor-injecting the SCOPED
/// IReadRepository&lt;TranslationView&gt;: this provider is reachable from singletons
/// (DbStringLocalizerFactory → ILocalizationProvider), and a ctor-captured scoped
/// repository would be a captive dependency — rejected outright by ValidateScopes
/// (Development), silently leaking a root-scoped DbContext elsewhere.
/// Translations are global reference data, so not joining the request scope is fine
/// (and CacheLocalizationProvider caches on top of this anyway).
/// </summary>
public class DbLocalizationProvider : ILocalizationProvider
{
    private readonly IServiceScopeFactory _scopes;

    public DbLocalizationProvider(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task<string?> GetString(string resourceKey, string locale)
    {
        using var scope = _scopes.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadRepository<TranslationView>>();
        return (await repo.GetAll(new TranslationViewFilter().ByLocale(locale).ByKey(resourceKey))).FirstOrDefault()?.Value;
    }

    public async Task<Dictionary<string, string>> GetStrings(string locale)
    {
        using var scope = _scopes.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadRepository<TranslationView>>();
        return (await repo.GetAll(new TranslationViewFilter().ByLocale(locale))).ToDictionary(t => t.Key, t => t.Value);
    }

    public async Task<Dictionary<string, string>> GetStringsByGroup(string group, string locale)
    {
        using var scope = _scopes.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IReadRepository<TranslationView>>();
        return (await repo.GetAll(new TranslationViewFilter().ByLocale(locale).ByKeyStartsWith(group))).ToDictionary(t => t.Key, t => t.Value);
    }
}

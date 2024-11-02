namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IServiceCollection AddI18n(this IServiceCollection services, Action<DbLocalizationOptions> setupAction = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        AddI18nServices(services);

        if (setupAction is not null)
            services.Configure(setupAction);

        return services;
    }

    internal static IServiceCollection AddI18nServices(this IServiceCollection services)
    {
        //services.AddTransient<IEntityExporter<Resource>, EntityExporter<Resource, Filter<Resource, string>, string>>();
        //services.AddTransient<IEntityExporter<Translation>, EntityExporter<Translation, TranslationFilter, int>>();
        //services.AddTransient<IEntityFormatter, SqlMergeFormatter>();

        services.AddTransient<DbLocalizationProvider>();

        services.AddTransient<ILocalizationProvider>((serviceProvider) =>
        {
            var dbProvider = serviceProvider.GetService<DbLocalizationProvider>();

            var localizationProviders = new List<ILocalizationProvider>() { dbProvider };

            var options = serviceProvider.GetService<IOptions<DbLocalizationOptions>>();
            foreach (var provider in options.Value.Providers)
            {
                var service = serviceProvider.GetService(provider);
                localizationProviders.Add((ILocalizationProvider)service);
            }

            var aggregator = new CacheLocalizationProvider(new LocalizationProviderAggregator(localizationProviders), cacheLevel: 2);

            return aggregator;
        });

        services.AddTransient<ISupportedLanguagesProvider, DbSupportedLanguagesProvider>();

        services.TryAdd(new ServiceDescriptor(
            typeof(IStringLocalizerFactory),
            typeof(DbStringLocalizerFactory),
            ServiceLifetime.Singleton));

        services.TryAdd(new ServiceDescriptor(
            typeof(IStringLocalizer),
            typeof(DbStringLocalizer),
            ServiceLifetime.Scoped));

        services.AddTransient<ITranslateService, TranslateService>();

        //services.AddTransient<IExporter, Exporter>();
        //services.AddTransient<IExcelExporter, ExcelExporter>();
        //services.AddTransient<IImporter, Importer>();

        //services.AddSingleton(provider => new ExportEntityConfig<Translation>()
        //{
        //    MergeSearchCondition = new[] { nameof(Translation.LanguageId), nameof(Translation.ResourceId) },
        //    Ignore = new[] { nameof(Translation.Id) },
        //});
        //services.AddTransient(typeof(ExportEntityConfig<Resource>));

        return services;
    }
}

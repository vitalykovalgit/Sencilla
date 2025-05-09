﻿namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IServiceCollection AddI18n(this IServiceCollection services, Action<Sencilla.Component.I18n.LocalizationOptions>? setupAction = null)
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
        services.AddTransient<DbLocalizationProvider>();
        services.AddTransient<ILocalizationProvider>((serviceProvider) =>
        {
            var dbProvider = serviceProvider.GetService<DbLocalizationProvider>();
            var localizationProviders = new List<ILocalizationProvider>() { dbProvider! };
            var aggregator = new CacheLocalizationProvider(new LocalizationProviderAggregator(localizationProviders), cacheLevel: 2);
            return aggregator;
        });

        services.AddTransient<ITranslateService, TranslateService>();

        // TODO: Review scoped lifetime
        services.TryAdd(new ServiceDescriptor(typeof(IStringLocalizerFactory), typeof(DbStringLocalizerFactory), ServiceLifetime.Singleton));
        services.TryAdd(new ServiceDescriptor(typeof(IStringLocalizer), typeof(DbStringLocalizer), ServiceLifetime.Scoped));
        return services;
    }
}

namespace Microsoft.Extensions.DependencyInjection;

public static class GoogleTranslatorBuilder
{
    public static IServiceCollection AddGoogleTranslator(this IServiceCollection services, Action<GoogleTranslatorOptions> setupAction = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (setupAction is null)
            throw new ArgumentNullException(nameof(setupAction));

        services.AddTransient<ITranslator, GoogleTranslator>();
        services.Configure(setupAction);

        return services;
    }
}

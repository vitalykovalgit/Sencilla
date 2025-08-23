namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
[DisableInjection]
public class MessagingConfig : ProviderConfig
{
    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public List<Action<IApplicationBuilder?, IHost?>> AppBuilders { get; } = [];

    /// <summary>
    /// The service collection for this provider.
    /// </summary>
    /// <value></value>
    public IServiceCollection Services { get; private set; } = null!;

    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public List<Type> Middlewares { get; } = [];


    /// <summary>
    /// Adds a middleware to the messaging pipeline only once.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public MessagingConfig AddMiddlewareOnce<T>() where T : class, IMessageMiddleware
    {
        if (Middlewares.Contains(typeof(T)))
            return this;

        Services.AddSingleton<T>();
        Middlewares.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Add stream provider to the messaging pipeline only once.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public MessagingConfig AddStreamProviderOnce<T>() where T : class, IMessageStreamProvider
    {
        // check if service already registered
        if (Services.Any(sd => sd.ServiceType == typeof(T)))
            return this;

        Services.AddSingleton<T>();
        return this;
    }

    public MessagingConfig AddHostedServiceOnce<T>(ProviderConfig config) where T : class, IHostedService
    {
        // check if service already registered
        if (Services.Any(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationType == typeof(T)))
            return this;

        if (config.Consumers.HasAnyConsumers)
            Services.AddHostedService<T>();

        return this;
    }

    public T? GetProviderConfig<T>() where T : ProviderConfig
    {
        return Services.FirstOrDefault(sd => sd.ServiceType == typeof(T))?.ImplementationInstance as T;
    }

    /// <summary>
    /// Adds a provider configuration to the messaging pipeline.
    /// </summary>
    /// <param name="config"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T AddProviderConfigOnce<T>(Action<T>? config) where T : ProviderConfig, new()
    {
        // get options instance
        var providerConfig = GetProviderConfig<T>();
        var options = providerConfig ?? new T();

        // invoke config action
        config?.Invoke(options);

        // Add only if not exists
        if (providerConfig == null)
            Services.AddSingleton(options);

        return options;
    }

    public bool IsTypeRegistered<T>() where T : class
    {
        return Services.Any(sd => sd.ServiceType == typeof(T));
    }

    public bool IsTypeNotRegistered<T>() where T : class
    {
        return !IsTypeRegistered<T>();
    }

    public MessagingConfig AddAppBuilderOnce(Action<IApplicationBuilder?, IHost?> builder)
    {
        if (AppBuilders.Contains(builder))
            return this;

        AppBuilders.Add(builder);
        return this;
    }

    /// <summary>
    /// Build app only once
    /// </summary>
    /// <param name="app"></param>
    public void BuildApp(IApplicationBuilder app)
    {
        if (AppBuilders.Count == 0)
            return;

        AppBuilders.ForEach(builder => builder.Invoke(app, null));
        AppBuilders.Clear();
    }

    public void BuildApp(IHost app)
    {
        if (AppBuilders.Count == 0)
            return;

        AppBuilders.ForEach(builder => builder.Invoke(null, app));
        AppBuilders.Clear();
    }

    internal void Init(IServiceCollection services)
    {
        Services = services;
    }

    internal void Cleanup()
    {
        Services = null!; // Clear the services collection to prevent further modifications
    }

}
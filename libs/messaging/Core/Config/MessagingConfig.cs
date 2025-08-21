namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
[DisableInjection]
public class MessagingConfig(IServiceCollection services) : ProviderConfig
{
    /// <summary>
    /// The service collection for this provider.
    /// </summary>
    /// <value></value>
    public IServiceCollection Services { get; private set; } = services;

    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public List<Type> Middlewares { get; } = [];

    /// <summary>
    /// Adds a middleware to the messaging pipeline.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public MessagingConfig AddMiddleware<T>() where T : class, IMessageMiddleware
    {
        Services.AddSingleton<T>();
        Middlewares.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a provider configuration to the messaging pipeline.
    /// </summary>
    /// <param name="config"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T AddProviderConfig<T>(Action<T>? config) where T : ProviderConfig, new()
    {
        var providerConfig = new T();
        config?.Invoke(providerConfig);
        Services.AddSingleton(providerConfig);
        return providerConfig;
    }

    internal void Cleanup()
    {
        Services = null!; // Clear the services collection to prevent further modifications
    }

}
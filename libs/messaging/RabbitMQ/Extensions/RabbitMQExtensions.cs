namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring RabbitMQ messaging
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Add RabbitMQ messaging services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Configuration action for RabbitMQ options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRabbitMQMessaging(
        this IServiceCollection services, 
        Action<RabbitMQOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<RabbitMQOptions>(_ => { });
        }

        // Register core RabbitMQ services
        services.AddSingleton<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>();
        services.AddSingleton<IRabbitMQTopologyManager, RabbitMQTopologyManager>();
        services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();
        services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();

        // Register middleware
        //services.AddTransient<IMessageMiddleware, RabbitMQMiddleware>();

        // Register hosted service for consuming messages
        services.AddHostedService<RabbitMQHostedService>();

        // Register health check
        services.AddHealthChecks()
            .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: new[] { "messaging", "rabbitmq" });

        return services;
    }

    /// <summary>
    /// Add RabbitMQ messaging services with configuration from configuration section
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="sectionName">Configuration section name (default: "RabbitMQ")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRabbitMQMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "RabbitMQ")
    {
        services.Configure<RabbitMQOptions>(configuration.GetSection(sectionName));
        return services.AddRabbitMQMessaging();
    }
}

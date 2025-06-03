namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ connection factory implementation
/// </summary>
public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory, IDisposable
{
    readonly ConnectionFactory _connectionFactory;
    readonly ILogger<RabbitMQConnectionFactory> _logger;
    readonly RabbitMQOptions _options;
    IConnection? _connection;
    readonly object _lockObject = new();

    public RabbitMQConnectionFactory(IOptions<RabbitMQOptions> options, ILogger<RabbitMQConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(_options.ConnectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60)
        };
    }

    public IConnection CreateConnection()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            lock (_lockObject)
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    try
                    {
                        _connection?.Dispose();
                        _connection = _connectionFactory.CreateConnection();
                        _logger.LogInformation("RabbitMQ connection established");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create RabbitMQ connection");
                        throw;
                    }
                }
            }
        }

        return _connection;
    }

    public IModel CreateChannel()
    {
        var connection = CreateConnection();
        var channel = connection.CreateModel();
        
        // Set basic QoS
        channel.BasicQos(0, _options.PrefetchCount, false);
        
        return channel;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ connection factory implementation
/// </summary>
public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory, IAsyncDisposable, IDisposable
{
    readonly ConnectionFactory _connectionFactory;
    readonly ILogger<RabbitMQConnectionFactory> _logger;
    readonly RabbitMQOptions _options;
    IConnection? _connection;
    readonly SemaphoreSlim _semaphore = new(1, 1);

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

    public async Task<IConnection> CreateConnectionAsync()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    try
                    {
                        if (_connection != null)
                        {
                            await _connection.DisposeAsync();
                        }
                        _connection = await _connectionFactory.CreateConnectionAsync();
                        _logger.LogInformation("RabbitMQ connection established");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create RabbitMQ connection");
                        throw;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return _connection;
    }

    public async Task<IChannel> CreateChannelAsync()
    {
        var connection = await CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        
        // Set basic QoS
        await channel.BasicQosAsync(0, _options.PrefetchCount, false);
        
        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
        _semaphore.Dispose();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _semaphore.Dispose();
    }
}

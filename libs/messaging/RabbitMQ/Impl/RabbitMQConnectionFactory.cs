namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQConnectionFactory(
    RabbitMQProviderOptions options,
    ILogger<RabbitMQConnectionFactory> logger) : IRabbitMQConnectionFactory, IAsyncDisposable, IDisposable
{
    private readonly ConnectionFactory Factory = new()
    {
        Uri = new Uri(options.ConnectionString),
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        RequestedHeartbeat = TimeSpan.FromSeconds(60)
    };

    private IConnection? Connection;
    private readonly SemaphoreSlim Semaphore = new(1, 1);

    public async Task<IConnection> CreateConnectionAsync()
    {
        if (Connection is not null && Connection.IsOpen)
            return Connection;

        await Semaphore.WaitAsync();
        try
        {
            if (Connection is not null && Connection.IsOpen)
                return Connection;

            if (Connection is not null)
                await Connection.DisposeAsync();

            Connection = await Factory.CreateConnectionAsync();
            logger.LogInformation("RabbitMQ connection established");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create RabbitMQ connection");
            throw;
        }
        finally
        {
            Semaphore.Release();
        }

        return Connection;
    }

    public async Task<IChannel> CreateChannelAsync()
    {
        var connection = await CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        await channel.BasicQosAsync(0, options.PrefetchCount, false);
        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (Connection is not null)
            await Connection.DisposeAsync();

        Semaphore.Dispose();
    }

    public void Dispose()
    {
        Connection?.Dispose();
        Semaphore.Dispose();
    }
}

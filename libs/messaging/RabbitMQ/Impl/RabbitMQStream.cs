namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQStream : IMessageStream, IAsyncDisposable
{
    private readonly IRabbitMQConnectionFactory ConnectionFactory;
    private readonly RabbitMQProviderOptions Options;
    private readonly StreamConfig StreamConfig;
    private readonly Channel<string> Buffer = Channel.CreateUnbounded<string>();

    private IChannel? PublishChannel;
    private IChannel? ConsumeChannel;
    private readonly SemaphoreSlim InitLock = new(1, 1);
    private bool ConsumerInitialized;
    private bool TopologyDeclared;

    public string Name { get; }

    public RabbitMQStream(IRabbitMQConnectionFactory connectionFactory, StreamConfig streamConfig, RabbitMQProviderOptions options)
    {
        ConnectionFactory = connectionFactory;
        StreamConfig = streamConfig;
        Options = options;
        Name = streamConfig.Name ?? throw new ArgumentNullException(nameof(streamConfig.Name));
    }

    public async Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        if (message is null) return;

        await EnsureTopologyAsync();

        var channel = await GetPublishChannelAsync();
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = StreamConfig.Durable,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            MessageId = message.Id.ToString(),
            Type = message.Namespace
        };

        if (Options.MessageTtlMilliseconds > 0)
            properties.Expiration = Options.MessageTtlMilliseconds.ToString();

        var exchange = StreamConfig.Topic == true ? Name : "";
        var routingKey = StreamConfig.Topic == true ? "" : Name;

        await channel.BasicPublishAsync(exchange, routingKey, false, properties, body, cancellationToken);
    }

    public async Task<string?> Read(CancellationToken cancellationToken = default)
    {
        await EnsureConsumerAsync();
        return await Buffer.Reader.ReadAsync(cancellationToken);
    }

    public async Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default)
    {
        var json = await Read(cancellationToken);
        if (json is null) return default;
        return JsonSerializer.Deserialize<Message<T>>(json);
    }

    private async Task EnsureTopologyAsync()
    {
        if (TopologyDeclared || !Options.AutoDeclareTopology) return;

        await InitLock.WaitAsync();
        try
        {
            if (TopologyDeclared) return;

            await using var channel = await ConnectionFactory.CreateChannelAsync();

            if (StreamConfig.Topic == true)
            {
                await channel.ExchangeDeclareAsync(Name, ExchangeType.Fanout, StreamConfig.Durable);
            }
            else
            {
                var arguments = BuildQueueArguments();
                await channel.QueueDeclareAsync(Name, StreamConfig.Durable, false, false, arguments);

                if (Options.EnableDeadLetterQueue)
                    await SetupDeadLetterTopologyAsync(channel);
            }

            TopologyDeclared = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    private async Task EnsureConsumerAsync()
    {
        if (ConsumerInitialized) return;

        await InitLock.WaitAsync();
        try
        {
            if (ConsumerInitialized) return;

            await EnsureTopologyAsync();

            ConsumeChannel = await ConnectionFactory.CreateChannelAsync();
            var consumer = new AsyncEventingBasicConsumer(ConsumeChannel);

            consumer.ReceivedAsync += async (_, args) =>
            {
                var json = Encoding.UTF8.GetString(args.Body.Span);
                await Buffer.Writer.WriteAsync(json);
            };

            var queueName = Name;

            // For topics, create a subscription queue bound to the exchange
            if (StreamConfig.Topic == true)
            {
                var subscriptionQueue = await ConsumeChannel.QueueDeclareAsync(
                    "", false, true, true);
                await ConsumeChannel.QueueBindAsync(subscriptionQueue.QueueName, Name, "");
                queueName = subscriptionQueue.QueueName;
            }

            await ConsumeChannel.BasicConsumeAsync(queueName, true, consumer);
            ConsumerInitialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    private async Task<IChannel> GetPublishChannelAsync()
    {
        if (PublishChannel is not null) return PublishChannel;

        await InitLock.WaitAsync();
        try
        {
            PublishChannel ??= await ConnectionFactory.CreateChannelAsync();
            return PublishChannel;
        }
        finally
        {
            InitLock.Release();
        }
    }

    private Dictionary<string, object?> BuildQueueArguments()
    {
        var args = new Dictionary<string, object?>();

        if (Options.MessageTtlMilliseconds > 0)
            args["x-message-ttl"] = Options.MessageTtlMilliseconds;

        if (Options.EnableDeadLetterQueue)
        {
            args["x-dead-letter-exchange"] = Options.DeadLetterExchange;
            args["x-dead-letter-routing-key"] = Name;
        }

        return args;
    }

    private async Task SetupDeadLetterTopologyAsync(IChannel channel)
    {
        var dlxName = Options.DeadLetterExchange;
        var dlqName = $"{Name}.{Options.DeadLetterQueue}";

        await channel.ExchangeDeclareAsync(dlxName, ExchangeType.Direct, true);
        await channel.QueueDeclareAsync(dlqName, true, false, false);
        await channel.QueueBindAsync(dlqName, dlxName, Name);
    }

    public async ValueTask DisposeAsync()
    {
        Buffer.Writer.TryComplete();

        if (PublishChannel is not null)
            await PublishChannel.DisposeAsync();

        if (ConsumeChannel is not null)
            await ConsumeChannel.DisposeAsync();

        InitLock.Dispose();
    }
}

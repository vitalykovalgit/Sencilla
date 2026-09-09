namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// One RabbitMQ queue (or fanout exchange) behind <see cref="IMessageStream"/>.
///
/// <para><b>Acknowledgement.</b> Deliveries are consumed with manual ack unless the consumer opts into
/// <see cref="ConsumerConfig.AutoAck"/>. The broker acknowledges by delivery tag on the channel that
/// received the message, while <see cref="IMessageStreamAck"/> is keyed by message id, so each
/// in-flight delivery is held in <see cref="Pending"/> until the consumer reports its outcome.</para>
///
/// <para><b>Why manual ack matters here.</b> Under autoAck the broker considers a message delivered the
/// moment it is written to the socket — before it reaches the buffer below, let alone a handler — so a
/// handler failure or a process restart loses it with no redelivery. It also makes <c>basic.qos</c>
/// inert (prefetch bounds UNACKNOWLEDGED deliveries, and nothing is ever unacknowledged), which lets
/// the broker push an entire backlog into this process's memory.</para>
///
/// <para><b>Redelivery.</b> A retryable failure is requeued once; a second failure, and every
/// non-retryable one, is rejected without requeue so it reaches the dead-letter exchange rather than
/// looping forever. That is also what makes the DLQ declared in <see cref="SetupDeadLetterTopologyAsync"/>
/// reachable at all — nothing dead-letters under autoAck.</para>
/// </summary>
public class RabbitMQStream : IMessageStream, IMessageStreamAck, IAsyncDisposable
{
    private readonly IRabbitMQConnectionFactory ConnectionFactory;
    private readonly RabbitMQProviderOptions Options;
    private readonly StreamConfig StreamConfig;
    private readonly ConsumerConfig? ConsumerConfig;
    private readonly ILogger Logger;
    private readonly Channel<string> Buffer = Channel.CreateUnbounded<string>();

    /// <summary>In-flight deliveries awaiting an outcome, keyed by the envelope id the consumer reports.</summary>
    private readonly ConcurrentDictionary<Guid, (ulong Tag, bool Redelivered)> Pending = [];

    private IChannel? PublishChannel;
    private IChannel? ConsumeChannel;
    private readonly SemaphoreSlim InitLock = new(1, 1);

    /// <summary>IChannel is not safe for concurrent use; MaxConcurrentHandlers &gt; 1 acks in parallel.</summary>
    private readonly SemaphoreSlim AckLock = new(1, 1);

    private bool ConsumerInitialized;
    private bool TopologyDeclared;

    public string Name { get; }

    /// <summary>Fire-and-forget delivery — opt-in, because it cannot survive a handler failure.</summary>
    private bool AutoAck => ConsumerConfig?.AutoAck ?? false;

    public RabbitMQStream(
        IRabbitMQConnectionFactory connectionFactory,
        StreamConfig streamConfig,
        RabbitMQProviderOptions options,
        ConsumerConfig? consumerConfig,
        ILogger logger)
    {
        ConnectionFactory = connectionFactory;
        StreamConfig = streamConfig;
        Options = options;
        ConsumerConfig = consumerConfig;
        Logger = logger;
        Name = streamConfig.Name ?? throw new ArgumentNullException(nameof(streamConfig.Name));
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        if (message is null) return;

        await EnsureTopologyAsync();

        var channel = await GetPublishChannelAsync();

        // MessageJson.Options, not the serializer defaults: every transport must put the same bytes on
        // the wire for the same envelope, or a DLQ dump (or any non-.NET reader) sees a different shape
        // per transport.
        var json = JsonSerializer.Serialize(message, MessageJson.Options);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = StreamConfig.Durable,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            MessageId = message.Id.ToString(),
            // PayloadType is the resolution key; Namespace is the legacy fallback.
            Type = message.PayloadType ?? message.Namespace
        };

        if (Options.MessageTtlMilliseconds > 0)
            properties.Expiration = Options.MessageTtlMilliseconds.ToString();

        var exchange = StreamConfig.Topic == true ? Name : "";
        var routingKey = StreamConfig.Topic == true ? "" : Name;

        await channel.BasicPublishAsync(exchange, routingKey, false, properties, body, cancellationToken);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<string?> Read(CancellationToken cancellationToken = default)
    {
        await EnsureConsumerAsync();
        return await Buffer.Reader.ReadAsync(cancellationToken);
    }

    public async Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default)
    {
        var json = await Read(cancellationToken);
        if (json is null) return default;
        return JsonSerializer.Deserialize<Message<T>>(json, MessageJson.Options);
    }

    // ── Acknowledgement ───────────────────────────────────────────────────────

    public async Task Ack(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (AutoAck) return;

        if (!Pending.TryRemove(messageId, out var delivery))
        {
            Logger.LogWarning("No in-flight delivery for message {MessageId} on stream {Stream}; nothing to ack", messageId, Name);
            return;
        }

        await WithChannel(ch => ch.BasicAckAsync(delivery.Tag, false, cancellationToken), messageId, "ack");
    }

    public async Task<bool> Nack(Guid messageId, string error, bool retryable, CancellationToken cancellationToken = default)
    {
        // Under autoAck the broker released the message on delivery, so there is nothing left to
        // requeue — the failure is already terminal and the caller should say so exactly once.
        if (AutoAck)
        {
            Logger.LogError("Message {MessageId} on stream {Stream} failed under autoAck and cannot be redelivered: {Error}",
                messageId, Name, error);
            return true;
        }

        if (!Pending.TryRemove(messageId, out var delivery))
        {
            Logger.LogWarning("No in-flight delivery for message {MessageId} on stream {Stream}; cannot nack", messageId, Name);
            return false;
        }

        // One requeue, then dead-letter. RabbitMQ carries no attempt counter on a classic queue, so
        // requeueing on every retryable failure is an infinite loop for a poison message; Redelivered
        // is the one bit of history the broker does give us.
        var requeue = retryable && !delivery.Redelivered;

        await WithChannel(ch => ch.BasicNackAsync(delivery.Tag, false, requeue, cancellationToken), messageId, "nack");

        Logger.LogWarning("Message {MessageId} on stream {Stream} failed: {Error} — {Outcome}",
            messageId, Name, error, requeue ? "requeued" : "rejected to the dead-letter exchange");

        return !requeue;
    }

    /// <summary>
    /// Acks and nacks go on the channel that delivered the message and must not overlap. A stale tag
    /// after an automatic reconnect throws here; that is survivable — the broker requeues everything
    /// unacknowledged when the connection drops, so the message comes back on its own.
    /// </summary>
    private async Task WithChannel(Func<IChannel, ValueTask> operation, Guid messageId, string what)
    {
        var channel = ConsumeChannel;
        if (channel is null)
        {
            Logger.LogWarning("Consume channel is gone; cannot {What} message {MessageId} on stream {Stream}", what, messageId, Name);
            return;
        }

        await AckLock.WaitAsync();
        try
        {
            await operation(channel);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to {What} message {MessageId} on stream {Stream}; leaving it to broker redelivery",
                what, messageId, Name);
        }
        finally
        {
            AckLock.Release();
        }
    }

    // ── Topology and consumer ─────────────────────────────────────────────────

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

        // BEFORE taking InitLock, never under it: EnsureTopologyAsync takes the same lock, and a
        // SemaphoreSlim is not reentrant. A stream that only ever consumes — the Notifier's — reached
        // this with the topology still undeclared (a producer declares it on its first Write, and there
        // is no producer here) and waited on itself forever: no connection, no queue, no error, just a
        // consumer that "started" and never read. Found 2026-09-09 with the first real RabbitMQ consumer.
        await EnsureTopologyAsync();

        await InitLock.WaitAsync();
        try
        {
            if (ConsumerInitialized) return;

            ConsumeChannel = await ConnectionFactory.CreateChannelAsync();
            var consumer = new AsyncEventingBasicConsumer(ConsumeChannel);

            consumer.ReceivedAsync += (_, args) => OnReceivedAsync(args);

            var queueName = Name;

            // For topics, create a subscription queue bound to the exchange
            if (StreamConfig.Topic == true)
            {
                var subscriptionQueue = await ConsumeChannel.QueueDeclareAsync(
                    "", false, true, true);
                await ConsumeChannel.QueueBindAsync(subscriptionQueue.QueueName, Name, "");
                queueName = subscriptionQueue.QueueName;
            }

            await ConsumeChannel.BasicConsumeAsync(
                queueName,
                AutoAck,
                consumerTag: "",
                noLocal: false,
                exclusive: ConsumerConfig?.Exclusive ?? false,
                arguments: null,
                consumer);

            ConsumerInitialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs args)
    {
        var json = Encoding.UTF8.GetString(args.Body.Span);

        if (!AutoAck)
        {
            var id = MessageIdOf(args, json);
            if (id is null)
            {
                // Nothing to correlate an ack against later. Dropping it would lose a message a handler
                // could process, so take it at-most-once instead: release the delivery now and carry on.
                Logger.LogWarning("Delivery {Tag} on stream {Stream} carries no message id; acknowledging on receipt (at-most-once)",
                    args.DeliveryTag, Name);
                await WithChannel(ch => ch.BasicAckAsync(args.DeliveryTag, false, CancellationToken.None), Guid.Empty, "ack");
            }
            else
            {
                Pending[id.Value] = (args.DeliveryTag, args.Redelivered);
            }
        }

        try
        {
            await Buffer.Writer.WriteAsync(json);
        }
        catch (ChannelClosedException)
        {
            // Shutting down. Under manual ack the delivery is still outstanding, so requeue it rather
            // than let it die in a buffer nobody will read.
            Logger.LogWarning("Buffer closed while receiving delivery {Tag} on stream {Stream}", args.DeliveryTag, Name);

            if (!AutoAck)
                await WithChannel(ch => ch.BasicNackAsync(args.DeliveryTag, false, true, CancellationToken.None), Guid.Empty, "nack");
        }
    }

    /// <summary>
    /// The id the consumer will report back. The AMQP header is authoritative for anything this library
    /// published; the body is probed for anything else, in both casings the envelope has been written in.
    /// </summary>
    private static Guid? MessageIdOf(BasicDeliverEventArgs args, string json)
    {
        if (Guid.TryParse(args.BasicProperties.MessageId, out var fromHeader))
            return fromHeader;

        try
        {
            using var document = JsonDocument.Parse(json);

            foreach (var name in (string[])["id", "Id"])
                if (document.RootElement.TryGetProperty(name, out var element)
                    && Guid.TryParse(element.GetString(), out var fromBody))
                    return fromBody;
        }
        catch (JsonException)
        {
            // Unparsable body — the consumer will report it; we just cannot key an ack on it.
        }

        return null;
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
        AckLock.Dispose();
    }
}

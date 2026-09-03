namespace Sencilla.Messaging;

public class MessageStreamConsumer(
    ILogger<MessageStreamConsumer> logger,
    IServiceScopeFactory scopeFactory,
    IMessageHandlerExecutor executor,
    IMessageStreamProvider provider,
    ConsumerConfig consumerConfig,
    StreamConfig streamConfig,
    string? tag = null)
{
    private static readonly MethodInfo BaseExecuteMethod =
        typeof(IMessageHandlerExecutor).GetMethod(nameof(IMessageHandlerExecutor.ExecuteAsync))!;

    private static readonly MethodInfo BaseRaiseFailedMethod =
        typeof(MessageStreamConsumer).GetMethod(nameof(RaiseFailedAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> ExecuteMethodCache = [];
    private static readonly ConcurrentDictionary<Type, MethodInfo> RaiseFailedMethodCache = [];

    private readonly SemaphoreSlim Semaphore = new(consumerConfig.MaxConcurrentHandlers);

    public async Task Execute(CancellationToken stoppingToken)
    {
        var consumerTag = tag ?? consumerConfig.StreamName ?? "default";

        logger.LogInformation("Consumer {Tag} started for stream {Stream} at {Time}",
            consumerTag, consumerConfig.StreamName, DateTimeOffset.UtcNow);

        var reader = provider.GetOrCreateStream(streamConfig);
        var inflightTasks = new List<Task>();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var json = await reader.Read(stoppingToken);
                if (json is null)
                {
                    // The contract is that Read blocks until it has something, so null means a transient
                    // miss (two readers racing for one channel item). A stream that returned null
                    // persistently would otherwise spin this loop at 100% CPU.
                    await Task.Delay(1, stoppingToken);
                    continue;
                }

                await Semaphore.WaitAsync(stoppingToken);

                var task = ProcessWithSemaphoreRelease(json, reader as IMessageStreamAck, stoppingToken);
                inflightTasks.Add(task);

                // Periodically clean completed tasks to avoid unbounded list growth
                if (inflightTasks.Count > consumerConfig.MaxConcurrentHandlers * 2)
                    inflightTasks.RemoveAll(t => t.IsCompleted);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Consumer {Tag} stopping for stream {Stream}",
                consumerTag, consumerConfig.StreamName);
        }

        // Graceful shutdown: wait for all in-flight messages to complete
        if (inflightTasks.Count > 0)
        {
            logger.LogInformation("Consumer {Tag} waiting for {Count} in-flight messages on stream {Stream}",
                consumerTag, inflightTasks.Count, consumerConfig.StreamName);
            await Task.WhenAll(inflightTasks);
        }

        logger.LogInformation("Consumer {Tag} stopped for stream {Stream} at {Time}",
            consumerTag, consumerConfig.StreamName, DateTimeOffset.UtcNow);
    }

    private async Task ProcessWithSemaphoreRelease(string json, IMessageStreamAck? ack, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessMessageAsync(json, ack, cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task ProcessMessageAsync(string json, IMessageStreamAck? ack, CancellationToken cancellationToken)
    {
        Message? message = null;
        Type? payloadType = null;
        object? typedMessage = null;

        try
        {
            message = JsonSerializer.Deserialize<Message>(json, MessageJson.Options);
            if (message is null)
            {
                // No envelope means no id to acknowledge — nothing to do but say so.
                logger.LogWarning("Received unparsable message on stream {Stream}, dropping", consumerConfig.StreamName);
                return;
            }

            // Namespace is the legacy field and stays a fallback for messages written before
            // PayloadType existed; new messages always carry PayloadType.
            var key = message.PayloadType ?? message.Namespace;
            if (string.IsNullOrEmpty(key))
            {
                await FailAsync(ack, message, "message.type.missing", retryable: false, cancellationToken);
                return;
            }

            payloadType = PayloadTypeRegistry.Resolve(key);
            if (payloadType is null)
            {
                // Never a silent skip: a durable row dropped here would stay claimed forever.
                // No T exists in this process, so no MessageFailed<T> can be raised either.
                await FailAsync(ack, message, $"message.type.unresolved: {key}", retryable: false, cancellationToken);
                return;
            }

            var genericMessageType = typeof(Message<>).MakeGenericType(payloadType);
            typedMessage = JsonSerializer.Deserialize(json, genericMessageType, MessageJson.Options);
            if (typedMessage is null)
            {
                await FailAsync(ack, message, $"message.payload.unparsable: {key}", retryable: false, cancellationToken);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            using var activity = StartActivity(message, payloadType);

            // Durable delivery is transactional: the handler's writes, anything it enqueues through
            // the scoped dispatcher, and the acknowledgement itself commit as one unit or not at all.
            // A crash after that commit cannot redeliver (the row is already terminal); a crash
            // before it rolls everything back, so the retry starts clean. Not opened for a
            // fire-and-forget stream (nothing to ack) nor on a host with no relational store.
            var transactions = ack is null ? null : scope.ServiceProvider.GetService<ITransactionFactory>();
            await using var transaction = transactions is null ? null : await transactions.Begin(cancellationToken);

            var executeMethod = ExecuteMethodCache.GetOrAdd(payloadType, type =>
                BaseExecuteMethod.MakeGenericMethod(type));

            var executed = await (Task<int>)executeMethod.Invoke(executor, [typedMessage, scope.ServiceProvider, cancellationToken])!;

            if (executed == 0)
            {
                // Resolvable but unhandled in this process — acking would lose the work silently.
                // The transaction is disposed uncommitted on the way out.
                await FailAsync(ack, message, $"message.handler.missing: {key}", retryable: false, cancellationToken, payloadType, typedMessage);
                return;
            }

            if (ack != null)
                await ack.Ack(message.Id, scope.ServiceProvider, CancellationToken.None);
            if (transaction != null)
                await transaction.CommitAsync(CancellationToken.None);

            logger.LogInformation("Processed message {MessageId} of type {Type} from stream {Stream}",
                message.Id, payloadType.Name, consumerConfig.StreamName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown, not failure: leave the message unacked so it is redelivered.
            logger.LogDebug("Message processing cancelled during shutdown on stream {Stream}", consumerConfig.StreamName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message from stream {Stream}", consumerConfig.StreamName);
            if (message != null)
                await FailAsync(ack, message, $"message.handler.failed: {ex.GetType().Name}: {ex.Message}",
                    retryable: true, CancellationToken.None, payloadType, typedMessage);
        }
    }

    /// <summary>
    /// Nack when the stream supports it; otherwise the log line is all there is. When the nack was
    /// terminal and the payload type resolved, raise <see cref="MessageFailed{T}"/> so the app can
    /// surface the failure — exactly once, after the row is already terminal.
    /// </summary>
    private async Task FailAsync(IMessageStreamAck? ack, Message message, string error, bool retryable,
        CancellationToken cancellationToken, Type? payloadType = null, object? typedMessage = null)
    {
        logger.LogError("Message {MessageId} on stream {Stream} failed: {Error} (retryable: {Retryable})",
            message.Id, consumerConfig.StreamName, error, retryable);

        if (ack == null)
            return;

        bool terminal;
        try
        {
            terminal = await ack.Nack(message.Id, error, retryable, cancellationToken);
        }
        catch (Exception ex)
        {
            // The nack itself can fail (the durable store is the thing that just broke). Swallowing it
            // here leaves the row claimed, which the transport's stuck-rescue re-queues by age — a
            // delayed retry. Letting it escape instead faults an unobserved task whose exception only
            // surfaces at shutdown, where it masks the graceful drain.
            logger.LogError(ex, "Could not nack message {MessageId} on stream {Stream}; leaving it for stuck-recovery",
                message.Id, consumerConfig.StreamName);
            return;
        }

        if (!terminal || payloadType is null || typedMessage is null)
            return;

        try
        {
            var raise = RaiseFailedMethodCache.GetOrAdd(payloadType, type => BaseRaiseFailedMethod.MakeGenericMethod(type));
            await (Task)raise.Invoke(this, [typedMessage, error, CancellationToken.None])!;
        }
        catch (Exception ex)
        {
            // A failing failure-handler must never mask the original failure.
            logger.LogError(ex, "MessageFailed<{Type}> handler threw for message {MessageId}", payloadType.Name, message.Id);
        }
    }

    /// <summary>
    /// Continue the sender's trace when the envelope carries one, so a request and the background
    /// work it queued show up as a single trace. Null when no listener has opted into the source.
    /// </summary>
    private static Activity? StartActivity(Message message, Type payloadType)
    {
        string? parent = null;
        message.Metadata?.TryGetValue(MessagingActivity.TraceParent, out parent);
        return MessagingActivity.Source.StartActivity($"{payloadType.Name} handle", ActivityKind.Consumer, parent);
    }

    private async Task RaiseFailedAsync<T>(Message<T> failedMessage, string error, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        await executor.ExecuteAsync(new Message<MessageFailed<T>>
        {
            CorrelationId = failedMessage.Id,
            Payload = new MessageFailed<T>
            {
                Message = failedMessage,
                Error = error,
                Attempts = failedMessage.Attempts,
            },
        }, scope.ServiceProvider, cancellationToken);
    }
}

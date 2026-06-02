namespace Sencilla.Web.Batch;

/// <summary>
/// Validates and runs a batch: GET steps first (outside any transaction), then write
/// steps in declared order inside an optional transaction, resolving <c>$ref</c>
/// substitutions and mapping failures to per-step statuses.
/// </summary>
internal sealed class BatchExecutor(
    IServiceProvider services,
    IBatchEntityRegistry registry,
    IBatchIdempotencyStore idempotency,
    BatchOptions options,
    ILogger<BatchExecutor> logger) : IBatchExecutor
{
    public async Task<BatchResult> ExecuteAsync(BatchRequest request, CancellationToken token = default)
    {
        var errors = BatchValidator.Validate(request, registry, options);
        if (errors.Count > 0)
            return BatchResult.Invalid(errors);

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var cached = await idempotency.GetAsync(request.IdempotencyKey, token);
            if (cached is not null)
            {
                cached.FromCache = true;
                return cached;
            }
        }

        var batchId = Guid.NewGuid().ToString("n");
        var byRef = new Dictionary<string, BatchStepResult>(StringComparer.Ordinal);

        // Partition while preserving order; both ops + descriptors are known-valid here.
        var gets = new List<PlannedStep>();
        var writes = new List<PlannedStep>();
        foreach (var step in request.Steps)
        {
            BatchOps.TryParse(step.Op, out var op);
            registry.TryGet(step.Entity, out var descriptor);
            (op.IsRead() ? gets : writes).Add(new PlannedStep(step, op, descriptor));
        }

        // GET steps first — never abort the batch.
        foreach (var planned in gets)
            byRef[planned.Step.Ref] = await RunReadAsync(planned, batchId, token);

        var success = await RunWritesAsync(writes, request.Transactional, byRef, batchId, token);

        var result = new BatchResult
        {
            Success = success.Ok,
            RolledBack = success.RolledBack,
            Steps = request.Steps.Select(s => byRef[s.Ref]).ToList(),
        };

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
            await idempotency.SetAsync(request.IdempotencyKey, result, options.IdempotencyTtl, token);

        return result;
    }

    private async Task<(bool Ok, bool RolledBack)> RunWritesAsync(
        List<PlannedStep> writes, bool transactional, Dictionary<string, BatchStepResult> byRef, string batchId, CancellationToken token)
    {
        if (writes.Count == 0)
            return (true, false);

        var writeResults = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var ok = true;
        var rolledBack = false;
        IDbTransaction? transaction = null;

        try
        {
            if (transactional)
                transaction = await writes[0].Descriptor.Invoker.BeginTransactionAsync(services, token);

            var aborted = false;
            foreach (var planned in writes)
            {
                if (aborted)
                {
                    byRef[planned.Step.Ref] = Skipped(planned);
                    continue;
                }

                try
                {
                    byRef[planned.Step.Ref] = await RunWriteAsync(planned, writeResults, batchId, token);
                }
                catch (Exception ex)
                {
                    ok = false;
                    aborted = true;
                    byRef[planned.Step.Ref] = Failed(planned, ex);
                    logger.LogError(ex, "Batch {BatchId} step {Ref} ({Op} {Entity}) failed", batchId, planned.Step.Ref, planned.Op, planned.Step.Entity);
                }
            }

            if (ok)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(token);
            }
            else if (transaction is not null)
            {
                await transaction.RollbackAsync(token);
                rolledBack = true;
            }
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }

        return (ok, rolledBack);
    }

    private async Task<BatchStepResult> RunWriteAsync(PlannedStep planned, Dictionary<string, JsonObject> writeResults, string batchId, CancellationToken token)
    {
        var body = AsObject(planned.Step.Body);
        if (body is not null)
            BatchRefResolver.Resolve(body, writeResults);

        var outcome = await InvokeAsync(planned, body, batchId, token);
        var result = Success(planned, outcome);

        if (outcome.Data is not null && JsonSerializer.SerializeToNode(outcome.Data, options.Json) is JsonObject node)
            writeResults[planned.Step.Ref] = node;

        return result;
    }

    private async Task<BatchStepResult> RunReadAsync(PlannedStep planned, string batchId, CancellationToken token)
    {
        try
        {
            var outcome = await InvokeAsync(planned, body: null, batchId, token);
            return Success(planned, outcome);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch {BatchId} read step {Ref} ({Op} {Entity}) failed", batchId, planned.Step.Ref, planned.Op, planned.Step.Entity);
            return Failed(planned, ex);
        }
    }

    private async Task<BatchOpOutcome> InvokeAsync(PlannedStep planned, JsonObject? body, string batchId, CancellationToken token)
    {
        var sw = Stopwatch.StartNew();

        IFilter? filter = null;
        if (planned.Op is BatchOp.GetAll or BatchOp.GetCount && planned.Step.Filter is { } f)
            filter = f.Deserialize<Filter>(options.Json);

        var outcome = await planned.Descriptor.Invoker.InvokeAsync(planned.Op, services, body, planned.Step.Id, filter, options.Json, token);

        sw.Stop();
        logger.LogInformation("Batch {BatchId} step {Ref} {Op} {Entity} -> {Status} in {Ms}ms",
            batchId, planned.Step.Ref, planned.Op, planned.Step.Entity, outcome.Status, sw.ElapsedMilliseconds);

        return outcome;
    }

    private static JsonObject? AsObject(JsonElement? body)
        => body is { ValueKind: JsonValueKind.Object } element
            ? JsonNode.Parse(element.GetRawText()) as JsonObject
            : null;

    private static BatchStepResult Success(PlannedStep planned, BatchOpOutcome outcome) => new()
    {
        Ref = planned.Step.Ref,
        Op = planned.Step.Op,
        Entity = planned.Step.Entity,
        Status = outcome.Status,
        Data = outcome.Data,
    };

    private static BatchStepResult Failed(PlannedStep planned, Exception ex) => new()
    {
        Ref = planned.Step.Ref,
        Op = planned.Step.Op,
        Entity = planned.Step.Entity,
        Status = MapStatus(ex),
        Error = ex.Message,
    };

    private static BatchStepResult Skipped(PlannedStep planned) => new()
    {
        Ref = planned.Step.Ref,
        Op = planned.Step.Op,
        Entity = planned.Step.Entity,
        Status = 0,
        Skipped = true,
    };

    private static int MapStatus(Exception ex) => ex switch
    {
        UnauthorizedException => 401,
        ForbiddenException => 403,
        BatchOperationNotSupportedException => 501,
        BatchRefException => 400,
        _ => 500,
    };

    private readonly record struct PlannedStep(BatchStep Step, BatchOp Op, BatchEntityDescriptor Descriptor);
}

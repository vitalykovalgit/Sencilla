namespace Sencilla.Web.Batch;

/// <summary>Outcome of one repository invocation: an HTTP-style status and the data.</summary>
internal readonly record struct BatchOpOutcome(int Status, object? Data);

/// <summary>
/// Non-generic facade over an entity's strongly-typed repositories. One instance per
/// <c>[SencillaEntity]</c> type, created at startup. Keeps the executor free of
/// per-op reflection — the only reflection is constructing the generic implementation.
/// </summary>
internal interface IBatchEntityInvoker
{
    /// <summary>Opens a transaction on the entity's (shared) DbContext.</summary>
    Task<IDbTransaction> BeginTransactionAsync(IServiceProvider services, CancellationToken token);

    /// <summary>Runs one operation, resolving the repository from <paramref name="services"/>.</summary>
    Task<BatchOpOutcome> InvokeAsync(
        BatchOp op,
        IServiceProvider services,
        JsonObject? body,
        JsonElement? id,
        IFilter? filter,
        JsonSerializerOptions json,
        CancellationToken token);
}

/// <summary>Thrown when an entity flags an op but the backing repository isn't registered.</summary>
internal sealed class BatchOperationNotSupportedException(string message) : Exception(message);

/// <summary>Thrown when a <c>$ref</c> can't be resolved at execution time.</summary>
internal sealed class BatchRefException(string message) : Exception(message);

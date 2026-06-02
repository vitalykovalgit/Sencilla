namespace Sencilla.Web.Batch;

/// <summary>
/// Validates and executes a <see cref="BatchRequest"/>.
/// </summary>
public interface IBatchExecutor
{
    Task<BatchResult> ExecuteAsync(BatchRequest request, CancellationToken token = default);
}

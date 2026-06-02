namespace Sencilla.Web.Batch;

/// <summary>
/// Single endpoint that executes a batch of dependent CRUD/GET steps.
/// </summary>
/// <remarks>
/// v1 has no per-step authorization — it runs under the host's default auth and is
/// equivalent in trust to the direct CRUD endpoints. See PRD §9.
/// </remarks>
[ApiController]
[Route("api/v1/batch")]
public sealed class BatchController(IBatchExecutor executor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Execute([FromBody] BatchRequest request, CancellationToken token)
    {
        var result = await executor.ExecuteAsync(request, token);

        // Pre-flight rejection (nothing ran) -> 400. Execution outcomes (incl. a
        // rolled-back batch) -> 200 with structured per-step detail.
        return result.ValidationErrors is { Count: > 0 }
            ? BadRequest(result)
            : Ok(result);
    }
}

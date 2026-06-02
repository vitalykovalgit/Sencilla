global using System.Diagnostics;
global using System.Reflection;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Text.Json.Serialization;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.ApplicationParts;

global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Logging;

global using Sencilla.Core;
global using Sencilla.Web.Batch;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration entry point for the Sencilla batch API.
/// </summary>
public static class Bootstrap
{
    /// <summary>
    /// Registers the batch executor, entity registry, idempotency store and the
    /// <c>POST /api/v1/batch</c> controller. Call right after <c>AddSencillaWeb</c>:
    /// <code>
    /// var mvc = services.AddControllers();
    /// services.AddSencillaWeb(mvc);
    /// services.AddSencillaBatch(mvc);
    /// </code>
    /// </summary>
    public static IServiceCollection AddSencillaBatch(this IServiceCollection services, IMvcBuilder mvc, Action<BatchOptions>? configure = null)
    {
        var options = new BatchOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        // Entity registry is built lazily on first resolve so every entity assembly
        // is guaranteed loaded by then (mirrors CrudApiControllerFeatureProvider's
        // AppDomain scan timing).
        services.TryAddSingleton<IBatchEntityRegistry>(_ => BatchEntityRegistry.BuildFromAppDomain());

        // Default single-instance idempotency store. Override with a distributed
        // (Redis) implementation for multi-pod deployments.
        services.TryAddSingleton<IBatchIdempotencyStore, MemoryCacheBatchIdempotencyStore>();

        services.TryAddScoped<IBatchExecutor, BatchExecutor>();

        // The BatchController lives in this assembly, not the host's. Register the
        // assembly as an MVC application part so the controller is discovered.
        mvc.AddApplicationPart(typeof(BatchController).Assembly);

        return services;
    }
}

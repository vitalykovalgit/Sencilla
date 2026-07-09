namespace Microsoft.Extensions.DependencyInjection;

public static class AuditApplicationBuilderEx
{
    /// <summary>Reads the X-Audit-Reason header into the per-request audit context. Chain after <c>UseSencillaUser()</c>.</summary>
    public static IApplicationBuilder UseSencillaAudit(this IApplicationBuilder builder)
        => builder.UseMiddleware<AuditReasonMiddleware>();
}

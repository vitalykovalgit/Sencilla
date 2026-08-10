using Microsoft.AspNetCore.Builder;
using Sencilla.Component.Security;

namespace Microsoft.Extensions.DependencyInjection;

public static class SecurityApplicationBuilderEx
{
    /// <summary>
    /// Re-points the current user at the impersonated account when a valid impersonation cookie is
    /// present. Chain AFTER <c>UseSencillaUser()</c> — it needs the real user already resolved — and
    /// BEFORE <c>UseSencillaAudit()</c> and the endpoints, so everything downstream sees one
    /// consistent identity.
    /// </summary>
    public static IApplicationBuilder UseSencillaImpersonation(this IApplicationBuilder builder)
        => builder.UseMiddleware<ImpersonationMiddleware>();
}

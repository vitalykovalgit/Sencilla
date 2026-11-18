
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///
/// </summary>
public static class ApplicationBuilderEx
{
    /// <summary>
    /// Makes each request's current user the account its session carries (<see cref="CurrentUserMiddleware"/>).
    /// Chain after <c>UseAuthentication()</c>.
    /// </summary>
    public static IApplicationBuilder UseSencillaUser(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CurrentUserMiddleware>();
    }
}

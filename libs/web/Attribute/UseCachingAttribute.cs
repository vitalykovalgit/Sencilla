namespace Sencilla.Web;

/// <summary>
/// Enables memory caching for entity queries with configurable timeout
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UseCachingAttribute : Attribute
{
    /// <summary>
    /// Creates a caching attribute with the specified timeout
    /// </summary>
    /// <param name="timeoutMinutes">Cache timeout in minutes (default: 5 minutes)</param>
    public UseCachingAttribute(int timeoutMinutes = 5)
    {
        TimeoutMinutes = timeoutMinutes;
    }

    /// <summary>
    /// Cache timeout in minutes
    /// </summary>
    public int TimeoutMinutes { get; }

    /// <summary>
    /// Gets the cache expiration as a TimeSpan
    /// </summary>
    public TimeSpan Expiration => TimeSpan.FromMinutes(TimeoutMinutes);
}

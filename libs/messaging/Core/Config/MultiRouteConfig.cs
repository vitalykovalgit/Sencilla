namespace Sencilla.Messaging;

/// <summary>
/// A composite route configuration that applies stream destinations to multiple message types at once.
/// Used by <see cref="RoutesConfig.Messages"/> to support the fluent pattern:
/// <c>r.Messages(typeof(A), typeof(B)).To("queue1");</c>
/// </summary>
public class MultiRouteConfig(IReadOnlyList<RouteConfig> routes)
{
    /// <summary>
    /// Routes all configured message types to the specified streams.
    /// </summary>
    public void To(params IEnumerable<string> streams)
    {
        foreach (var route in routes)
            route.To(streams);
    }

    /// <summary>
    /// Routes all configured message types to the specified streams.
    /// </summary>
    public void ToStream(params IEnumerable<string> streams)
    {
        foreach (var route in routes)
            route.ToStream(streams);
    }
}

namespace Sencilla.Messaging.Mediator;

public class MediatorConfig : ProviderConfig
{
    private readonly HashSet<Type> AllowedTypes = [];
    private readonly HashSet<Type> DisabledTypes = [];
    private bool AllowAllFlag = true;

    /// <summary>
    /// Allow all message types to be handled (default behavior).
    /// </summary>
    public MediatorConfig AllowAll()
    {
        AllowAllFlag = true;
        DisabledTypes.Clear();
        return this;
    }

    /// <summary>
    /// Disable all message types from being handled.
    /// Use Allow&lt;T&gt;() to selectively re-enable specific types.
    /// </summary>
    public MediatorConfig DisableAll()
    {
        AllowAllFlag = false;
        AllowedTypes.Clear();
        return this;
    }

    /// <summary>
    /// Allow a specific message type to be handled (used after DisableAll).
    /// </summary>
    public MediatorConfig Allow<T>()
    {
        AllowedTypes.Add(typeof(T));
        DisabledTypes.Remove(typeof(T));
        return this;
    }

    /// <summary>
    /// Allow specific message types to be handled.
    /// </summary>
    public MediatorConfig Allow(params Type[] types)
    {
        foreach (var type in types)
        {
            AllowedTypes.Add(type);
            DisabledTypes.Remove(type);
        }
        return this;
    }

    /// <summary>
    /// Disable a specific message type from being handled.
    /// </summary>
    public MediatorConfig Disable<T>()
    {
        DisabledTypes.Add(typeof(T));
        AllowedTypes.Remove(typeof(T));
        return this;
    }

    /// <summary>
    /// Disable specific message types from being handled.
    /// </summary>
    public MediatorConfig Disable(params Type[] types)
    {
        foreach (var type in types)
        {
            DisabledTypes.Add(type);
            AllowedTypes.Remove(type);
        }
        return this;
    }

    /// <summary>
    /// Determines whether a message of the given type should be handled.
    /// </summary>
    public bool ShouldHandle(Type type)
    {
        if (AllowAllFlag)
            return !DisabledTypes.Contains(type);

        return AllowedTypes.Contains(type);
    }
}

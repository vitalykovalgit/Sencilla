namespace Sencilla.Component.Security;

/// <summary>
/// 
/// </summary>
public class Access : IDisposable
{
    /// <summary>
    /// Contains stack with all acesses 
    /// </summary>
    private static AsyncLocal<Stack<Access>> Stack = new AsyncLocal<Stack<Access>>();

    /// <summary>
    /// Retrive current 
    /// </summary>
    public static Access? Current => (Stack?.Value?.Count ?? 0) > 0 ? Stack?.Value?.Peek() : null;

    public bool AllowAll { get; private set; }

    public Guid ContextId { get; private set; }

    private Access(bool allowAll)
    {
        ContextId = Guid.NewGuid();
        AllowAll = allowAll;

        Stack.Value ??= new Stack<Access>();
        Stack.Value.Push(this);
    }

    public static Access Root()
    {
        return new Access(true);
    }

    public void Dispose()
    {
        Stack?.Value?.Pop();
    }
}

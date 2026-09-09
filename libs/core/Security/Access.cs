namespace Sencilla.Core;

/// <summary>
/// Ambient "acting as the system" scope. While one is open on the current async flow, entity security
/// lets every read and write through — the calling code, not a user, is the authorization: a message
/// transport claiming its own rows, role provisioning inside a create, an audit trail writing itself.
/// Lives in Core so infrastructure that must never know about users or roles can still declare a
/// system action; the Security component reads <see cref="Current"/> and stays the only enforcer.
///
/// Scopes nest and are per async flow: a child task inherits the scope open when it started and
/// cannot see what its siblings open or close afterwards, so concurrent handlers never share state.
/// <code>
/// using var root = Access.Root();
/// await messages.Update(claimed, token);   // no ForbiddenException, whoever the current user is
/// </code>
/// </summary>
public sealed class Access : IDisposable
{
    private static readonly AsyncLocal<Access?> Ambient = new();

    /// <summary>The innermost open scope on this async flow; null when code runs as the current user.</summary>
    public static Access? Current => Ambient.Value;

    public bool AllowAll { get; }

    private readonly Access? parent;

    private Access(bool allowAll)
    {
        AllowAll = allowAll;
        parent = Ambient.Value;
        Ambient.Value = this;
    }

    /// <summary>Open a scope that bypasses entity security until disposed.</summary>
    public static Access Root() => new(true);

    public void Dispose() => Ambient.Value = parent;
}

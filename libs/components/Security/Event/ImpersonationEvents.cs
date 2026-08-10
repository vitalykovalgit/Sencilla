namespace Sencilla.Component.Security;

/// <summary>
/// An impersonation session beginning or ending, announced so that components which may not be
/// referenced from here can react — the audit log above all.
///
/// The direction of that dependency is the whole reason these exist. <c>Sencilla.Component.Audit</c>
/// already references Security (its own trusted insert needs <c>Access.Root()</c>), so Security cannot
/// reference Audit back to write the rows itself. Publishing instead of writing also means a host that
/// does not install the audit component still gets a working endpoint rather than a missing type.
///
/// Both users are carried whole rather than as ids: every subscriber so far needs the email, and a
/// handler re-reading the row would do it as the impersonated identity, which is exactly the identity
/// that must not appear as the actor.
/// </summary>
public abstract class ImpersonationEvent : Event
{
    /// <summary>The real operator — the human who decided. Never the impersonated account.</summary>
    public required User Operator { get; init; }

    /// <summary>The account being browsed as.</summary>
    public required User Target { get; init; }
}

/// <summary>An operator began acting as <see cref="ImpersonationEvent.Target"/>.</summary>
public class ImpersonationStartedEvent : ImpersonationEvent { }

/// <summary>
/// An operator stopped acting as <see cref="ImpersonationEvent.Target"/>.
///
/// Raised only for a session that was actually live — clearing an already-clear cookie is a success,
/// not an event, or a double-clicked exit would log a stop that never happened.
/// </summary>
public class ImpersonationStoppedEvent : ImpersonationEvent { }

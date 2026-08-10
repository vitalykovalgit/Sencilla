namespace Sencilla.Component.Security;

/// <summary>
/// Per-request record of an active impersonation: who is really calling, and as whom.
///
/// The effective identity itself lives where it always has — <c>ISystemVariable.GetCurrentUser()</c> —
/// so row scoping, role resolution, permission checks and CreatedBy/UpdatedBy stamps need no knowledge
/// of impersonation at all. This context exists purely so the parts that must NOT be fooled (audit
/// attribution, the banner) can still see the real actor behind the swap.
///
/// Empty on an ordinary request; <see cref="IsImpersonating"/> is false and every id is null.
/// </summary>
public interface IImpersonationContext
{
    /// <summary>True when this request is running as someone other than the authenticated principal.</summary>
    bool IsImpersonating { get; }

    /// <summary>The REAL caller — the user who authenticated and started the impersonation.</summary>
    Guid? ImpersonatorId { get; }

    /// <summary>The real caller's email, for the banner and for audit payloads.</summary>
    string? ImpersonatorEmail { get; }

    /// <summary>The user being impersonated (the request's effective identity).</summary>
    Guid? TargetId { get; }

    /// <summary>
    /// Records the swap. Called once per request by <see cref="ImpersonationMiddleware"/> AFTER it has
    /// verified the impersonator still holds the grant — never from application code.
    /// </summary>
    void Begin(User impersonator, User target);
}

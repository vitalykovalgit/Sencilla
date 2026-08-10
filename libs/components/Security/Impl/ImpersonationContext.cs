namespace Sencilla.Component.Security;

/// <summary>Contract and rationale in <see cref="IImpersonationContext"/>.</summary>
[PerRequestLifetime]
public class ImpersonationContext : IImpersonationContext
{
    public bool IsImpersonating => ImpersonatorId.HasValue;

    public Guid? ImpersonatorId { get; private set; }

    public string? ImpersonatorEmail { get; private set; }

    public Guid? TargetId { get; private set; }

    public void Begin(User impersonator, User target)
    {
        ImpersonatorId = impersonator.Id;
        ImpersonatorEmail = impersonator.Email;
        TargetId = target.Id;
    }
}

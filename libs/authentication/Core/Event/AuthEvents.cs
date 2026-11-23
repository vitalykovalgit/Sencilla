namespace Sencilla.Authentication;

/// <summary>
/// Authentication domain events published via <c>IEventDispatcher</c>. Hosts subscribe with
/// <c>IEventHandlerBase&lt;T&gt;</c> to send email/SMS, write the audit log, or emit telemetry —
/// keeping delivery and templating out of the authentication packages.
/// </summary>
public sealed class UserRegistered : Event
{
    public Guid UserId { get; init; }
    public string? Email { get; init; }
    public string Method { get; init; } = "password";
}

public sealed class LoginSucceeded : Event
{
    public Guid UserId { get; init; }
    public string Method { get; init; } = "password";
    public string? Ip { get; init; }

    /// <summary>
    /// The provider identity an external sign-in resolved — a new account, a known subject, a link by address or a
    /// connect — as its reader handed it over (<see cref="ExternalIdentity.RawClaims"/> included); null for a
    /// password sign-in.
    /// </summary>
    public ExternalIdentity? ExternalIdentity { get; init; }
}

public sealed class LoginFailed : Event
{
    public string? Email { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Ip { get; init; }
}

public sealed class EmailVerificationRequested : Event
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}

public sealed class PasswordResetRequested : Event
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}

public sealed class PasswordChanged : Event
{
    public Guid UserId { get; init; }

    /// <summary><c>changed</c> (by the user), <c>reset</c> (via an emailed link) or <c>removed</c> (a verified social login displaced an unverified password credential).</summary>
    public string Reason { get; init; } = "changed";

    /// <summary>For <c>removed</c>, the provider key whose verified address displaced the password (<c>google</c>, <c>apple</c>); null otherwise.</summary>
    public string? Provider { get; init; }
}

public sealed class ProviderLinked : Event
{
    public Guid UserId { get; init; }
    public string Provider { get; init; } = string.Empty;
}

public sealed class RefreshTokenRotated : Event
{
    public Guid UserId { get; init; }
}

public sealed class RefreshReuseDetected : Event
{
    public Guid UserId { get; init; }
}

public sealed class TokenRevoked : Event
{
    public Guid UserId { get; init; }
}

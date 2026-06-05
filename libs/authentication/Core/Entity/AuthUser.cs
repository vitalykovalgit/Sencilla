namespace Sencilla.Authentication;

/// <summary>
/// Account lifecycle status. Server rejects login for <see cref="Locked"/> / <see cref="Disabled"/>.
/// </summary>
public enum AuthUserStatus : byte
{
    Active = 1,
    PendingVerification = 2,
    Locked = 3,
    Disabled = 4,
}

/// <summary>
/// Minimal, immutable identity projection used across the authentication family. Hash-free by
/// design — credentials never ride on the user. The <see cref="IUserStore"/> implementation maps
/// the concrete user entity into this record. <see cref="Phone"/> is optional profile data that
/// enables phone sign-in when the store persists it (see <c>UseAppAccounts(o =&gt; o.AllowPhoneLogin())</c>).
/// </summary>
public sealed record AuthUser(Guid Id, string? Email, bool EmailConfirmed, AuthUserStatus Status, long? Phone = null);

namespace Sencilla.Component.Audit;

/// <summary>
/// Who performed a change, for audit attribution. <see cref="System"/> = no request actor (background job,
/// migration, seed); <see cref="User"/> = an end user (authenticated or anonymous customer); <see cref="Admin"/>
/// = a staff / admin-role user. Resolved per request from the current user; the who-audit primitive the audit
/// component stamps (fin/other CreatedBy stamps can fold onto it later).
/// </summary>
public enum ActorType : byte
{
    System = 0,
    User = 1,
    Admin = 2,
}

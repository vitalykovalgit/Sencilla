namespace Sencilla.Authentication;

/// <summary>
/// Grants the configured default roles to every newly registered user — password and social alike,
/// since both publish <see cref="UserRegistered"/>. Registered by
/// <c>UseAppAccounts(o =&gt; o.DefaultRole(...))</c>; a no-op when no default roles are configured.
/// </summary>
public sealed class DefaultRoleAssigner(IOptions<AppAccountsOptions> options) : IEventHandlerBase<UserRegistered>
{
    public async Task HandleAsync(UserRegistered @event, CancellationToken token, ICreateRepository<UserRole, Guid> roles)
    {
        foreach (var roleId in options.Value.DefaultRoles)
            await roles.Create(new UserRole { Id = Guid.NewGuid(), UserId = @event.UserId, RoleId = roleId }, token);
    }
}

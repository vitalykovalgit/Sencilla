namespace Sencilla.Authentication;

/// <summary>
/// Configures the email/password ("app accounts") feature (<c>UseAppAccounts</c>): password and
/// lockout policy, whether the login identifier may be a phone number, and the roles every new
/// user receives. Password policy binds from the <c>"Authentication:Password"</c> section, then the
/// <see cref="PasswordPolicy"/> override is applied.
/// </summary>
public sealed class AppAccountsConfig
{
    internal Action<PasswordPolicyOptions>? PasswordOverride { get; private set; }
    internal Action<LockoutOptions>? LockoutOverride { get; private set; }
    internal bool PhoneLogin { get; private set; }
    internal List<int> DefaultRoles { get; } = [];

    public AppAccountsConfig PasswordPolicy(Action<PasswordPolicyOptions> configure)
    {
        PasswordOverride = configure;
        return this;
    }

    public AppAccountsConfig Lockout(Action<LockoutOptions> configure)
    {
        LockoutOverride = configure;
        return this;
    }

    /// <summary>Allow a phone number as the login identifier (resolved via <see cref="IUserStore.FindByPhone"/>).</summary>
    public AppAccountsConfig AllowPhoneLogin(bool enabled = true)
    {
        PhoneLogin = enabled;
        return this;
    }

    /// <summary>Grant these roles to every newly registered user (password and social).</summary>
    public AppAccountsConfig DefaultRole(params RoleType[] roles)
    {
        DefaultRoles.AddRange(roles.Select(r => (int)r));
        return this;
    }

    /// <summary>Grant these role ids to every newly registered user (password and social).</summary>
    public AppAccountsConfig DefaultRole(params int[] roleIds)
    {
        DefaultRoles.AddRange(roleIds);
        return this;
    }
}

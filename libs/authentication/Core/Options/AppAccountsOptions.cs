namespace Sencilla.Authentication;

/// <summary>
/// Behaviour of the email/password ("app accounts") feature: password and lockout policy, whether the
/// login identifier may be a phone number, which roles every freshly registered user receives, how
/// login failures are reported, and how long the emailed reset / verification links stay valid.
/// Configured by <c>UseAppAccounts(o =&gt; ...)</c>; <see cref="Password"/> binds from the
/// <c>"Authentication:Password"</c> section before that lambda runs.
/// </summary>
public sealed class AppAccountsOptions
{
    /// <summary>Password policy and hashing cost (also served as <c>IOptions&lt;PasswordPolicyOptions&gt;</c>).</summary>
    public PasswordPolicyOptions Password { get; } = new();

    /// <summary>Brute-force lockout thresholds (also served as <c>IOptions&lt;LockoutOptions&gt;</c>).</summary>
    public LockoutOptions Lockout { get; } = new();

    /// <summary>When true, a non-email login identifier is resolved against the user's phone number.</summary>
    public bool PhoneLoginEnabled { get; set; }

    /// <summary>
    /// Role ids granted to every new user on registration (password and social). Maps to
    /// <c>Sencilla.Component.Users.RoleType</c> values; assigned by the registration event handler.
    /// </summary>
    public IList<int> DefaultRoles { get; set; } = new List<int>();

    /// <summary>
    /// When true, a failed login says WHY — <c>account-not-found</c>, <c>password-not-set</c> (the
    /// account exists but only has social logins) or <c>invalid-password</c> — instead of the uniform
    /// <c>invalid-credentials</c>. That is an account-enumeration oracle; enable it only for apps whose
    /// self-service registration already reveals whether an email is taken, and keep the built-in
    /// login limits (<see cref="AuthRateLimitOptions"/>) on.
    /// </summary>
    public bool DetailedLoginErrors { get; set; }

    /// <summary>How long an emailed password-reset link stays valid. Single use regardless.</summary>
    public TimeSpan ResetTokenLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>How long an emailed address-verification link stays valid. Single use regardless.</summary>
    public TimeSpan VerificationTokenLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Publish <see cref="EmailVerificationRequested"/> (with a fresh token) right after a password registration.</summary>
    public bool VerifyEmailOnRegister { get; set; } = true;
}

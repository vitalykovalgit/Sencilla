namespace Sencilla.Authentication;

/// <summary>
/// Behaviour of the email/password ("app accounts") feature beyond the password and lockout
/// policies: whether the login identifier may be a phone number, and which roles every freshly
/// registered user receives. Populated by <c>UseAppAccounts(o =&gt; ...)</c>.
/// </summary>
public sealed class AppAccountsOptions
{
    /// <summary>When true, a non-email login identifier is resolved against the user's phone number.</summary>
    public bool PhoneLoginEnabled { get; set; }

    /// <summary>
    /// Role ids granted to every new user on registration (password and social). Maps to
    /// <c>Sencilla.Component.Users.RoleType</c> values; assigned by the registration event handler.
    /// </summary>
    public IList<int> DefaultRoles { get; set; } = new List<int>();
}

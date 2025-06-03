namespace Sencilla.Component.Users.Auth;

/// <summary>
/// Represents a request to register a new user.
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public long Phone { get; set; } = default!;

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
}
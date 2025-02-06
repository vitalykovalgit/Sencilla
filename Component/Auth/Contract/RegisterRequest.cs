namespace Sencilla.Component.Users.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public long Phone { get; set; } = default!;

    public string FirstName { get; set; }
    public string LastName { get; set; }

}
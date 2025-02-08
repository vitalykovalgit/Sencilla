
namespace Sencilla.Component.Users.Auth;

public class UserRefreshTokenFilter : Filter
{
    public UserRefreshTokenFilter WithToken(params string[] tokens)
    {
        AddProperty(nameof(UserRefreshToken.Token), typeof(string), tokens);
        return this;
    }

    public static UserRefreshTokenFilter ByToken(params string[] tokens)
        => new UserRefreshTokenFilter().WithToken(tokens);
}

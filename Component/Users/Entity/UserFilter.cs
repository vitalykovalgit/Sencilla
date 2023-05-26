
namespace Sencilla.Component.Users;

public class UserFilter: Filter
{
    public UserFilter WithEmail(params string?[] emails) 
    {
        AddProperty(nameof(User.Email), typeof(string), emails);
        return this;
    }

    public static UserFilter ByEmail(params string?[] emails) => new UserFilter().WithEmail(emails);
}

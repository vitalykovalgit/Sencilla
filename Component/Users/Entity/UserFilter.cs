
namespace Sencilla.Component.Users;

public class UserFilter: Filter
{
    public UserFilter WithEmail(params string?[] emails) 
    {
        AddProperty(nameof(User.Email), typeof(string), emails);
        return this;
    }

    public static UserFilter ByEmail(params string?[] emails) => new UserFilter().WithEmail(emails);

    public UserFilter WithId(params int[] ids)
    {
        AddProperty(nameof(User.Id), typeof(int), ids);
        return this;
    }

    public static UserFilter ById(params int[] ids) => new UserFilter().WithId(ids);
}

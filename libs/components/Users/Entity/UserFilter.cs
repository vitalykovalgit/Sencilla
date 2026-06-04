
namespace Sencilla.Component.Users;

public class UserFilter: Filter
{
    public UserFilter WithEmail(params string?[] emails) 
    {
        AddProperty(nameof(User.Email), typeof(string), emails);
        return this;
    }

    public static UserFilter ByEmail(params string?[] emails) => new UserFilter().WithEmail(emails);

    public UserFilter WithId(params Guid[] ids)
    {
        AddProperty(nameof(User.Id), typeof(Guid), ids.Cast<object>().ToArray());
        return this;
    }

    public static UserFilter ById(params Guid[] ids) => new UserFilter().WithId(ids);
}

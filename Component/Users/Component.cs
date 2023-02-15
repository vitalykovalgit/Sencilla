
namespace Microsoft.Extensions.DependencyInjection;

public class UsersComponent : IComponent
{
    public string Type => "Sencilla.Users";

    public void Init(IContainer container)
    {
    }
}

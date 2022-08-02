using Sencilla.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public class UsersComponent : IComponent
    {
        public string Type => "Users";

        public void Init(IContainer container)
        {
        }
    }
}

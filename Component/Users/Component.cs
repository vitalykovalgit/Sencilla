using Sencilla.Core.Component;
using Sencilla.Core.Injection;
using Sencilla.Component.Users.Entity;
using Sencilla.Component.Users.Impl;

namespace Sencilla.Component.Users
{
    public class UsersComponent : IComponent
    {
        public string Type => "Users";

        public void Init(IResolver resolver)
        {
            resolver.AddRepositoriesFor<User, ulong, UsersDbContext>();
        }
    }
}

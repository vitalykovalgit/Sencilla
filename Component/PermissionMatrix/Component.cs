
using Sencilla.Core;
using Sencilla.Core.Component;
using Sencilla.Core.Injection;
using Sencilla.Component.Security.Impl;

namespace Sencilla.Component.Security
{
    public class SecurityComponent : IComponent
    {
        public string Type => nameof(SecurityComponent);

        public void Init(IResolver resolver)
        {
            resolver.RegisterType<IReadPermission, PermissionMatrixBl>();
            resolver.RegisterType<ICreatePermission, PermissionMatrixBl>();
            resolver.RegisterType<IUpdatePermission, PermissionMatrixBl>();
            resolver.RegisterType<IRemovePermission, PermissionMatrixBl>();
            resolver.RegisterType<IDeletePermission, PermissionMatrixBl>();
        }
    }
}

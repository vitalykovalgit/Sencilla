
namespace Sencilla.Core.Security.Impl
{
    class AllowAllPermissions
        : IReadPermission
        , ICreatePermission
        , IUpdatePermission
        , IRemovePermission
        , IDeletePermission
    {
        public bool CanCreate<TEntity>(TEntity entity)
        {
            return true;
        }

        public bool CanDelete<TEntity>(TEntity entity)
        {
            return true;
        }

        public bool CanRead<TEntity>(TEntity entity)
        {
            return true;
        }

        public bool CanRemove<TEntity>(TEntity entity)
        {
            return true;
        }

        public bool CanUpdate<TEntity>(TEntity entity)
        {
            return true;
        }
    }
}

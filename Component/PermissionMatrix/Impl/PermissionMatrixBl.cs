
using Sencilla.Core;

namespace Sencilla.Component.Security.Impl
{
    class PermissionMatrixBl
        : IReadPermission
        , ICreatePermission
        , IUpdatePermission
        , IRemovePermission
        , IDeletePermission
    {
        public PermissionMatrixBl()
        {
            
        }

        public bool CanRead<TEntity>(TEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public bool CanCreate<TEntity>(TEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public bool CanDelete<TEntity>(TEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public bool CanRemove<TEntity>(TEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public bool CanUpdate<TEntity>(TEntity entity)
        {
            throw new System.NotImplementedException();
        }
    }
}

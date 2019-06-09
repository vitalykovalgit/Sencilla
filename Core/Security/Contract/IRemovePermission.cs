
using Sencilla.Core.Entity;

namespace Sencilla.Core
{
    public interface IRemovePermission
    {
        bool CanRemove<TEntity>(TEntity entity);
    }

    public interface IRemovePermission<TEntity> : IRemovePermission
    {
        bool CanRemove(TEntity entity);
    }
}

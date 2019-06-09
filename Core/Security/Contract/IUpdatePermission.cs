
namespace Sencilla.Core
{
    public interface IUpdatePermission
    {
        bool CanUpdate<TEntity>(TEntity entity);
    }

    public interface IUpdatePermission<TEntity> : IUpdatePermission
    {
        bool CanUpdate(TEntity entity);
    }
}

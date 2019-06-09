
namespace Sencilla.Core
{
    public interface IDeletePermission
    {
        bool CanDelete<TEntity>(TEntity entity);
    }

    public interface IDeletePermission<TEntity> : IDeletePermission
    {
        bool CanDelete(TEntity entity);
    }
}


namespace Sencilla.Core
{
    public interface ICreatePermission
    {
        bool CanCreate<TEntity>(TEntity entity);
    }

    public interface ICreatePermission<TEntity> : ICreatePermission
    {
        bool CanCreate(TEntity entity);
    }
}

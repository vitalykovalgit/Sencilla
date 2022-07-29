

namespace Sencilla.Core
{
    public interface IReadPermission
    {
        bool CanRead<TEntity>(TEntity entity);
    }

    public interface IReadPermission<TEntity> : IReadPermission
    {
        bool CanRead(TEntity entity);
    }
}

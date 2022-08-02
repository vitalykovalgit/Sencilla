
namespace Sencilla.Core
{
    public interface IEntityConstraint
    {
        bool Apply<TEntity>(TEntity entity);
    }

}

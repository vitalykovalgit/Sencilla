
namespace Sencilla.Core
{
    public interface ICreateConstraint
    {
        bool Apply<TEntity>(TEntity entity);
    }

    public interface ICreateConstraint<TEntity>
    {
        bool Apply(TEntity entity);
    }
}


namespace Sencilla.Core
{
    public interface IReadConstraint
    {
        IQueryable<TEntity> Apply<TEntity>(IQueryable<TEntity> query, IFilter? filter = null);
    }

    //public interface IReadConstraint<TEntity>
    //{
    //    bool Apply(TEntity entity);
    //}

}

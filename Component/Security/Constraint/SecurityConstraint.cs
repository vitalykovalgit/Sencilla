
using Sencilla.Core;

namespace Sencilla.Component.Security
{
    [Implement(typeof(IReadConstraint))]
    public class SecurityConstraint: IReadConstraint
    {
        public IQueryable<TEntity> Apply<TEntity>(IQueryable<TEntity> query, IFilter? filter = null)
        {
            // current user
            var user = 1; // get current user and his roles
            var resource = typeof(TEntity).Name; // get from attribute also
            var action = Action.Read;

            // read matrix with security constraints
            
            // apply constraints
            //query = query.Where(e => EF.Property<object>(e, "Name").Equals("Milan"));

            return query;
        }
    }
}

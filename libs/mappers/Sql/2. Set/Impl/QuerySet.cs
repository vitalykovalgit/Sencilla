using Sencilla.Infrastructure.SqlMapper.Contract;
using Sencilla.Infrastructure.SqlMapper.Impl.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Set
{
    internal class QuerySet<TEntity, TKey> : DbSet<TEntity, TKey> where TEntity : class, new()
    {
        public QuerySet(DbContext context) : base(context)
        {
        }

        public override SelectQueryBuilder<TEntity> Select()
        {
            return new SelectQueryBuilder<TEntity>(Context, Table);
        }

        public override DeleteQueryBuilder<TEntity> Delete()
        {
            return new DeleteQueryBuilder<TEntity>(Context, Table);
        }

        public override UpdateQueryBuilder<TEntity> Update()
        {
            return new UpdateQueryBuilder<TEntity>(Context, Table);
        }

        public override InsertQueryBuilder<TEntity> Insert()
        {
            return new InsertQueryBuilder<TEntity>(Context, Table);
        }

        public override int Count()
        {
            return GetAll().Count;
        }

        public override int Count(IFilter filter)
        {
            throw new NotImplementedException();
        }

        public override Task<int> CountAsync(CancellationToken? token = null)
        {
            throw new NotImplementedException();
        }

        public override List<TEntity> GetAll(params Expression<Func<TEntity, object>>[] properties)
        {
            return Select().Include(properties).ToList(null);
        }

        public override Task<List<TEntity>> GetAllAsync(CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties)
        {
            return Select().ToListAsync(null, token);
        }

        public override TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] properties)
        {
            var _ = new QueryParams();
            return Select().Include(properties).Where($"{Table.PrimaryKey} = {_[id]}").FirstOrDefault(_);
        }

        public override Task<TEntity> GetByIdAsync(TKey id, CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties)
        {
            var _ = new QueryParams();
            return Select().Where($"{Table.PrimaryKey} = {_[id]}").FirstOrDefaultAsync(_, token);
        }

        public override List<TEntity> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] properties)
        {
            if (ids == null || !ids.Any())
            {
                return new List<TEntity>();
            }

            var _ = new QueryParams();

            return Select().Include(properties).Where($"{Table.PrimaryKey} IN ({_.AddAsArray(ids.ToArray())})").ToList(_);
        }

        public override List<TEntity> GetAll(IFilter filter, params Expression<Func<TEntity, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public override TKey Insert(TEntity entity)
        {
            var key = Insert().Entity(entity).ReadFirstColumn<TKey>();
            return key;
        }

        public override Task<TKey> InsertAsync(TEntity entity, CancellationToken? token = null)
        {
            var key = Insert().Entity(entity).ReadFirstColumnAsync<TKey>(token);
            return key;
        }

        public override int Update(TEntity entity, TKey id)
        {
            var _ = new QueryParams();
            return Update().Entity(entity).Where($"{Table.PrimaryKey.Reference} = {_[id]}").Execute(_);
        }

        public override Task<int> UpdateAsync(TEntity entity, TKey id, CancellationToken? token = null)
        {
            var _ = new QueryParams();
            return Update().Entity(entity).Where($"{Table.PrimaryKey.Reference} = {_[id]}").ExecuteAsync(_, token);
        }

        public override int Delete(TKey id)
        {
            var _ = new QueryParams();
            return Delete().Where($"{Table.PrimaryKey.Reference} = {_[id]}").Execute(_);
        }

        public override Task<int> DeleteAsync(TKey id, CancellationToken? token = null)
        {
            var _ = new QueryParams();
            return Delete().Where($"{Table.PrimaryKey.Reference} = {_[id]}").ExecuteAsync(_, token);
        }

        public override int DeleteMany(params TKey[] ids)
        {
            if (ids == null || !ids.Any())
            {
                return 0;
            }

            var _ = new QueryParams();
            
            return Delete().Where($"{Table.PrimaryKey.Reference} IN ({_.AddAsArray(ids)})").Execute(_);
        }

        public override int DeleteAll()
        {
            return Delete().Execute(null);
        }

        public override Task<int> DeleteAllAsync(CancellationToken? token = null)
        {
            return Delete().ExecuteAsync(null, token);
        }
    }
}

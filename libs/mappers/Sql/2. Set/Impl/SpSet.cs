using Sencilla.Infrastructure.SqlMapper.Contract;
using Sencilla.Infrastructure.SqlMapper.Impl.Expression;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Set
{
    internal class SpSet<TEntity, TKey> : DbSet<TEntity, TKey> where TEntity : class, new()
    {
        private const string FilterSuffix = "ByFilter";

        /// <summary>
        /// 
        /// </summary>
        protected Settings Settings => Settings.Instance;

        /// <summary>
        /// Stored procedure name in DB
        /// </summary>
        protected string SpNamePrefix { get; set; }

        public SpSet(DbContext context) : base(context)
        {
            SpNamePrefix = $"{(Settings?.SpPrefix ?? "crud_")}{Table.Schema}{Table.Name}";
        }

        public override SelectQueryBuilder<TEntity> Select()
        {
            throw new NotSupportedException($"{nameof(SelectQueryBuilder<TEntity>)} is not supported for {nameof(SpSet<TEntity, TKey>)}");
        }

        public override UpdateQueryBuilder<TEntity> Update()
        {
            throw new NotSupportedException($"{nameof(UpdateQueryBuilder<TEntity>)} is not supported for {nameof(SpSet<TEntity, TKey>)}");
        }

        public override InsertQueryBuilder<TEntity> Insert()
        {
            throw new NotSupportedException($"{nameof(InsertQueryBuilder<TEntity>)} is not supported for {nameof(SpSet<TEntity, TKey>)}");
        }

        public override DeleteQueryBuilder<TEntity> Delete()
        {
            throw new NotSupportedException($"{nameof(DeleteQueryBuilder<TEntity>)} is not supported for {nameof(SpSet<TEntity, TKey>)}");
        }

        public override int Count()
        {
            return (int)Context.ExecuteProcedureScalar(ProcedureName(nameof(Count)));
        }

        public override int Count(IFilter filter)
        {
            return (int)Context.ExecuteProcedureScalar(ProcedureName(nameof(Count) + FilterSuffix), Params(filter.CountParams));
        }

        public override Task<int> CountAsync(CancellationToken? token = null)
        {
            return Context.ExecuteProcedureReaderAsync(ProcedureName(nameof(Count)), token).ReadFirstColumnAsync<int>();
        }

        public override TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] properties)
        {
            return ReadFirstStoredProcedure(ProcedureName(nameof(GetById)), DbParam.Id(id), DbParam.Includes(Includes(properties)));
        }

        public override Task<TEntity> GetByIdAsync(TKey id, CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties)
        {
            return ReadFirstStoredProcedureAsync(ProcedureName(nameof(GetById)), token, DbParam.Id(id), DbParam.Includes(Includes(properties)));
        }

        public override List<TEntity> GetAll(params Expression<Func<TEntity, object>>[] properties)
        {
            return ReadStoredProcedure(ProcedureName(nameof(GetAll)), DbParam.Includes(Includes(properties)));
        }

        public override Task<List<TEntity>> GetAllAsync(CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties)
        {
            return ReadStoredProcedureAsync(ProcedureName(nameof(GetAll)), token, DbParam.Includes(Includes(properties)));
        }

        public override List<TEntity> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] properties)
        {
            return ReadStoredProcedure(ProcedureName(nameof(GetMany)), DbParam.Ids(Objects(ids)), DbParam.Includes(Includes(properties)));
        }

        public override List<TEntity> GetAll(IFilter filter, params Expression<Func<TEntity, object>>[] properties)
        {
            var dbParams = new List<DbParam>(filter.SelectParams) { DbParam.Includes(Includes(properties)) };

            return ReadStoredProcedure(ProcedureName(nameof(GetAll) + FilterSuffix), dbParams.ToArray());
        }

        public override TKey Insert(TEntity entity)
        {
            return Context.ExecuteProcedureReader(ProcedureName(nameof(Insert)), InsertEntityParams(entity)).ReadFirstColumn<TKey>();
        }

        public override Task<TKey> InsertAsync(TEntity entity, CancellationToken? token = null)
        {
            return Context.ExecuteProcedureReaderAsync(ProcedureName(nameof(Insert)), token, InsertEntityParams(entity)).ReadFirstColumnAsync<TKey>();
        }

        public override int Update(TEntity entity, TKey id)
        {
            return Context.ExecuteProcedure(ProcedureName(nameof(Update)), UpdateEntityParams(entity));
        }

        public override Task<int> UpdateAsync(TEntity entity, TKey id, CancellationToken? token = null)
        {
            return Context.ExecuteProcedureAsync(ProcedureName(nameof(Update)), token, UpdateEntityParams(entity));
        }

        public override int Delete(TKey id)
        {
            return RunStoredProcedure(ProcedureName(nameof(Delete)), DbParam.Id(id));
        }

        public override Task<int> DeleteAsync(TKey id, CancellationToken? token = null)
        {
            return RunStoredProcedureAsync(ProcedureName(nameof(Delete)), token, DbParam.Id(id));
        }

        public override int DeleteMany(params TKey[] id)
        {
            return RunStoredProcedure(ProcedureName(nameof(DeleteMany)), DbParam.Ids(Objects(id)));
        }

        public override int DeleteAll()
        {
            return RunStoredProcedure(ProcedureName(nameof(DeleteAll)));
        }

        public override Task<int> DeleteAllAsync(CancellationToken? token = null)
        {
            return RunStoredProcedureAsync(ProcedureName(nameof(DeleteAll)));
        }

        #region Private methods

        private string ProcedureName(string methodName)
        {
            return $"{SpNamePrefix}_{methodName}";
        }

        private object[] Objects(IEnumerable<TKey> values)
        {
            var objectIds = values.Cast<Object>().ToArray();

            return objectIds;
        }

        #endregion
    }
}

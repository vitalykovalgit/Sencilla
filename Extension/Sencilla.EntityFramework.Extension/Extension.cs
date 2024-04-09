namespace Sencilla.EntityFramework.Extension;

public static class EfExtension
{
    public static IQueryable<TEntity> Upsert<TEntity>(this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, bool>> condition,
        Expression<Func<TEntity, TEntity>> insertAction,
        Expression<Func<TEntity, TEntity, TEntity>> updateAction) where TEntity : class
    {
        var command = new UpsertCommand<TEntity>(condition)
        {
            InsertAction = insertAction,
            UpdateAction = updateAction,
        };

        var builder = new UpsertQueryBuilder<TEntity>();

        builder.Add(command);

        builder.Build();

    }

    public void ExecuteMergeQuery(IEnumerable<> entities,
        string tableName,
        Expression<Func<TEntity, bool>> mergeCondition,
        Expression<Func<TEntity, TEntity>> actionInsert,
        Expression<Func<TEntity, TEntity, TEntity>> actionUpdate)
    {
        

        builder.Add(command);

        var query = builder.Build(tableName, entities);

        // execute with context
        // todo: do as extension to db context upsert or upsertjson
        // do with single entity, one param - insert action, second for update
        // do merge as in photoboost - check [PhotoDiscountData]
        // pull latest changes, table splitting was added
    }
}

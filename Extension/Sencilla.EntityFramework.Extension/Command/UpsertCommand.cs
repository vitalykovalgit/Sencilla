namespace Sencilla.EntityFramework.Extension.Command;

public class UpsertCommand<TEntity>
{
    public Expression<Func<TEntity, bool>> MatchedCondition { get; private set; }

    public UpsertCommand(Expression<Func<TEntity, bool>> mc)
    {
        MatchedCondition = mc;
    }

    public Expression<Func<TEntity, TEntity>>? InsertAction { get; set; }

    public Expression<Func<TEntity, TEntity, TEntity>>? UpdateAction { get; set; }
}

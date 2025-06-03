namespace Sencilla.Repository.EntityFramework.Extension;

public class MergeCommand<TEntity>
{
    public Expression<Func<TEntity, object>> MatchedCondition { get; private set; }

    public MergeCommand(Expression<Func<TEntity, object>> mc)
    {
        MatchedCondition = mc;
    }

    public Expression<Func<TEntity, TEntity>>? InsertAction { get; set; }

    public Expression<Func<TEntity, TEntity>>? UpdateAction { get; set; }
}

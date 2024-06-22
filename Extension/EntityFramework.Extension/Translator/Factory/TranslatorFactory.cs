namespace Sencilla.Repository.EntityFramework.Extension;

public class TranslatorFactory
{
    public BaseQueryTranslator Get(QueryClauseType t)
    {
        return t switch
        {
            QueryClauseType.MergeMatchCondition => new MergeConditionClauseTranslator(),
            QueryClauseType.MergeUpdateCondition => new MergeUpdateClauseTranslator(),
            QueryClauseType.MergeInsertCondition => new MergeInsertClauseTranslator(),
            _ => throw new NotImplementedException(),
        };
    }
}

namespace Sencilla.Repository.EntityFramework.DbContextOptions;

public class BulkMergeOptions<TEntity>
{
    public Expression<Func<TEntity, object>> PKColumnExpression { get; set; }

    public System.Reflection.PropertyInfo PKColumn
    {
        get
        {
            if (_pKColumn == null)
            {
                var memberExpression = PKColumnExpression.Body as MemberExpression;
                if (memberExpression == null)
                {
                    throw new ArgumentException("Expression must be a MemberExpression", nameof(PKColumnExpression));
                }

                _pKColumn = memberExpression.Member as System.Reflection.PropertyInfo;
                if (_pKColumn == null)
                {
                    throw new ArgumentException("Expression must be a property", nameof(PKColumnExpression));
                }
            }

            return _pKColumn;
        }
    }

    private System.Reflection.PropertyInfo _pKColumn;
}

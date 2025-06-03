namespace Sencilla.Infrastructure.SqlMapper
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISqlExpression
    {
        /// <summary>
        /// Sql expression 
        /// </summary>
        string Sql { get; }

        /// <summary>
        /// The same as Sql property 
        /// </summary>
        /// <returns></returns>
        string ToString();
    }
}

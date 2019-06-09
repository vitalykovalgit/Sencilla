using Sencilla.Infrastructure.SqlMapper.Impl.Expression;

namespace Sencilla.Infrastructure.SqlMapper.Contract
{
    public interface IFilter
    {
        /// <summary>
        /// Filter values in params array including paging and ordering
        /// </summary>
        /// <returns></returns>
        DbParam[] SelectParams { get; }

        /// <summary>
        /// Filter values in params array without paging and ordering
        /// </summary>
        /// <returns></returns>
        DbParam[] CountParams { get; }
    }
}

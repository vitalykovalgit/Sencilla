using System.Collections.Generic;
using System.Data;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    /// <summary>
    /// DataTable class that is passed to SQL procedures as TInclude parameter
    /// </summary>
    class IncludeTable : DataTable
    {
        public IncludeTable() : base(ParamTypes.TInclude)
        {
            Columns.Add(nameof(Include.ParentTable), typeof(string));
            Columns.Add(nameof(Include.ParentKey), typeof(string));
            Columns.Add(nameof(Include.JoinedTable), typeof(string));
            Columns.Add(nameof(Include.JoinedKey), typeof(string));
        }

        public IncludeTable(IList<Include> includes) : this()
        {
            AddIncludes(includes);
        }

        public void AddIncludes(params Include[] includes)
        {
            foreach (var include in includes)
            {
                Rows.Add(include.ParentTable, include.ParentKey, include.JoinedTable, include.JoinedKey);
            }
        }

        public void AddIncludes(IList<Include> includes)
        {
            foreach (var include in includes)
            {
                Rows.Add(include.ParentTable, include.ParentKey, include.JoinedTable, include.JoinedKey);
            }
        }
    }
}

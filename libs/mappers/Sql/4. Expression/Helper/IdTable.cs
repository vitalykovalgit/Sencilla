using System.Data;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    /// <summary>
    /// DataTable class that is passed to SQL procedures as TId parameter
    /// </summary>
    class IdTable : DataTable
    {
        public IdTable() : base(ParamTypes.TId)
        {
            Columns.Add("Id", typeof(int));
        }

        public IdTable(params object[] ids) : this()
        {
            AddIds(ids);
        }

        public void AddIds(params object[] ids)
        {
            foreach (var id in ids)
            {
                Rows.Add(id);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{

    /// <inheritdoc />
    /// <summary>
    /// Contains mapping between field name and column mapping 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class ColumnCollection<TEntity> : Dictionary<string, IColumnMapping<TEntity>>, IColumnCollection<TEntity>, IColumnCollection
    {
        const string Separator = ",";

        private IEnumerable<IColumnMapping<TEntity>> orderedColumns = null;

        protected IEnumerable<IColumnMapping<TEntity>> OrderedColumns => orderedColumns ?? (orderedColumns = Values.OrderBy(c => c.Name));

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string Names => string.Join(Separator, OrderedColumns.Select(c => c.SafeName));

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string Params => string.Join(Separator, OrderedColumns.Select(c => c.Param));

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string AliasReferences => string.Join(Separator, OrderedColumns.Select(c => c.AliasReference));

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string Declarations => string.Join(Separator, OrderedColumns.Select(c => c.Declaration));

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string ReferenceParams => string.Join(Separator, OrderedColumns.Select(c=>$"{c.SafeName} = {c.Param}"));

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return Declarations;
        }

        public new IEnumerator<IColumnMapping> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator<IColumnMapping<TEntity>> IEnumerable<IColumnMapping<TEntity>>.GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public void SetupColumnIndexes()
        {
            var columnIndex = 0;
            foreach (var column in OrderedColumns)
                column.Index = columnIndex++;
        }

        public void Add(IColumnMapping<TEntity> column)
        {
            if (!string.IsNullOrEmpty(column?.FieldName))
                base[column.FieldName.ToLower()] = column;
        }

        public IColumnMapping GetColumn(string fieldName)
        {
            return base[fieldName.ToLower()];
        }
    }
}

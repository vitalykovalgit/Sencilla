namespace Sencilla.EntityFramework.Extension.Builder;

public class UpsertQueryBuilder<TEntity>
{
    private readonly UpsertCommand<TEntity> _upsertCommand;

    public UpsertQueryBuilder(UpsertCommand<TEntity> upsertCommand)
    {
        _upsertCommand = upsertCommand;
    }

    public string Build(TEntity entity)
    {
        // DataTable dataTable = CreateDataTable(entities);

        var mergeQuery = $"MERGE INTO {targetTableName} AS Target";

        foreach (var mergeCommand in _mergeCommands)
        {
            var matchedCondition = mergeCommand.MatchedCondition;
            mergeQuery += Environment.NewLine + "ON " + matchedCondition.ToString().Replace("AndAlso", "AND");

            if (mergeCommand.UpdateAction != null)
            {
                var updateExpression = mergeCommand.UpdateAction.Body.ToString();
                mergeQuery += Environment.NewLine + "WHEN MATCHED THEN UPDATE SET " + updateExpression;
            }

            if (mergeCommand.InsertAction != null)
            {
                var insertExpression = mergeCommand.InsertAction.Body.ToString();
                mergeQuery += Environment.NewLine + "WHEN NOT MATCHED THEN INSERT VALUES " + insertExpression;
            }
        }

        string dropTableQuery = $"DROP TABLE #{targetTableName}";

        return CreateTempTable(targetTableName, dataTable) + Environment.NewLine +
               mergeQuery + Environment.NewLine +
               dropTableQuery;
    }

    private DataTable CreateDataTable(IEnumerable<TEntity> entities)
    {
        var dataTable = new DataTable();
        var properties = typeof(TEntity).GetProperties();

        foreach (var property in properties)
        {
            dataTable.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
        }

        foreach (var entity in entities)
        {
            var values = properties.Select(property => property.GetValue(entity)).ToArray();
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }

    private string CreateTempTable(string tableName, DataTable dataTable)
    {
        string createTableQuery = $"CREATE TABLE #{tableName} (";

        for (int i = 0; i < dataTable.Columns.Count; i++)
        {
            createTableQuery += $"{dataTable.Columns[i].ColumnName} {GetDbType(dataTable.Columns[i])}";

            if (i < dataTable.Columns.Count - 1)
            {
                createTableQuery += ", ";
            }
        }

        createTableQuery += ")";

        return createTableQuery;
    }

    private string GetDbType(DataColumn column)
    {
        if (column.DataType == typeof(int))
        {
            return "INT";
        }
        else if (column.DataType == typeof(Guid))
        {
            return "UNIQUEIDENTIFIER";
        }
        else if (column.DataType == typeof(string))
        {
            return $"NVARCHAR({column.MaxLength})";
        }
        else
        {
            throw new ArgumentException($"Unsupported data type: {column.DataType}");
        }
    }
}

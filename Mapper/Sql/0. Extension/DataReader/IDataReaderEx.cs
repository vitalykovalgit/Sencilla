namespace System.Data
{
    public static class IDataReaderEx
    {
        /// <summary>
        /// Return true if column exists in data reader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static bool HasColumn(this IDataReader reader, string columnName)
        {
            return GetColumnIndex(reader, columnName).HasValue;
        }

        public static int? GetColumnIndex(this IDataReader reader, string columnName)
        {
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    if (columnName.Equals(reader.GetName(i), StringComparison.InvariantCultureIgnoreCase))
                        return i;
                }
            }
            return null;
        }
    }
}

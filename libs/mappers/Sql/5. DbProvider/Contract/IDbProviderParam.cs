
namespace System.Data.Common
{
    /// <summary>
    /// It creates <see cref="DbParameter"/> for specific providers, like SQL Server, SQLite, Oracle etc...
    /// </summary>
    public interface IDbProviderParam
    {
        /// <summary>
        /// Create parameter
        /// </summary>
        /// <returns></returns>
        DbParameter Create();

        /// <summary>
        /// Create parameter with name and value
        /// </summary>
        /// <param name="name">Param name</param>
        /// <param name="value">Param value</param>
        /// <returns></returns>
        DbParameter Create(string name, object value);


        /// <summary>
        /// Create parameter with name and value
        /// </summary>
        /// <param name="name">Param name</param>
        /// <param name="value">Param value</param>
        /// <param name="type">Param type</param>
        /// <returns></returns>
        DbParameter Create(string name, object value, Type type);

        /// <summary>
        /// Create parameter of structured type
        /// </summary>
        /// <param name="typeName">Name of the structured parameter type.</param>
        /// <returns></returns>
        DbParameter CreateStructured(string typeName);
    }
}

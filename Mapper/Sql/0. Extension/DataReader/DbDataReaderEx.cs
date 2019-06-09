using Sencilla.Infrastructure.SqlMapper.Mapping;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public static class DbDataReaderEx
    {
        public static List<T> ReadList<T>(this DbDataReader reader)
        {
            var obj = new List<T>();
            using (reader)
            {
                while (reader.Read())
                {
                    var value = (T)reader.GetValue(0);
                    obj.Add(value);
                }
            }
            return obj;
        }

        /// <summary>
        /// Read first column from first record and return it 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T ReadFirstColumn<T>(this DbDataReader reader)
        {
            var obj = default(T);
            using (reader)
            {
                while (reader.Read())
                {
                    obj = (T)reader.GetValue(0);
                    break;
                }
            }
            return obj;
        }

        /// <summary>
        /// Read first column from first record and return it 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<T> ReadFirstColumnAsync<T>(this DbDataReader reader, CancellationToken? token = null)
        {
            var obj = default(T);
            using (reader)
            {
                while (await reader.ReadAsync(token ?? CancellationToken.None))
                {
                    obj = (T)reader.GetValue(0);
                    break;
                }
            }
            return obj;
        }

        /// <summary>
        /// Read first column from first record and return it 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<T> ReadFirstColumnAsync<T>(this Task<DbDataReader> reader, CancellationToken? token = null)
        {
            return await ReadFirstColumnAsync<T>(await reader, token);
        }

        /// <summary>
        /// Read entity from SqlDataReader and return it 
        /// </summary>
        /// <typeparam name="TEntity"> Entity type  </typeparam>
        /// <param name="reader"> Sql Data Reader </param>
        /// <param name="table"> Table mapping from sql to entity </param>
        /// <returns> Entity instance or null </returns>
        public static TEntity ReadFirstOrDefault<TEntity>(this DbDataReader reader, ITableMapping<TEntity> table)
        {
            //using (reader)
            //{
            //    var entity = default(TEntity);
            //    while (reader.Read())
            //    {
            //        entity = table.ReadFirstOrDefault(reader);
            //        break;
            //    }
            //    return entity;
            //}
            return table.ReadFirstOrDefault(reader);
        }

        /// <summary>
        /// Read entity from SqlDataReader and return it 
        /// </summary>
        /// <typeparam name="TEntity"> Entity type  </typeparam>
        /// <param name="reader"> Sql Data Reader </param>
        /// <param name="table"> Table mapping from sql to entity </param>
        /// <param name="token"> Cancellation token </param>
        /// <returns> Entity instance or null </returns>
        public static Task<TEntity> ReadFirstOrDefaultAsync<TEntity>(this DbDataReader reader, ITableMapping<TEntity> table, CancellationToken? token = null)
        {
            return table.ReadFirstOrDefaultAsync(reader, token);
        }

        /// <summary>
        /// Read entity from SqlDataReader and return it 
        /// </summary>
        /// <typeparam name="TEntity"> Entity type  </typeparam>
        /// <param name="reader"> Sql Data Reader </param>
        /// <param name="table"> Table mapping from sql to entity </param>
        /// <param name="token"> Cancellation token </param>
        /// <returns> Entity instance or null </returns>
        public static async Task<TEntity> ReadFirstOrDefaultAsync<TEntity>(this Task<DbDataReader> reader, ITableMapping<TEntity> table, CancellationToken? token = null)
        {
            return await ReadFirstOrDefaultAsync(await reader, table, token);
        }

        /// <summary>
        /// Read list of entity from SqlDataReader and return it 
        /// </summary>
        /// <typeparam name="TEntity"> Entity type  </typeparam>
        /// <param name="reader"> Sql Data Reader </param>
        /// <param name="table"> Table mapping from sql to entity </param>
        /// <returns> Entity instance or null </returns>
        public static List<TEntity> ReadList<TEntity>(this DbDataReader reader, ITableMapping<TEntity> table)
        {
            return table.ReadList(reader);
        }

        /// <summary>
        /// Read list of entity from SqlDataReader and return it 
        /// </summary>
        /// <typeparam name="TEntity"> Entity type  </typeparam>
        /// <param name="reader"> Sql Data Reader </param>
        /// <param name="table"> Table mapping from sql to entity </param>
        /// <param name="token"></param>
        /// <returns> Entity instance or null </returns>
        public static Task<List<TEntity>> ReadListAsync<TEntity>(this DbDataReader reader, ITableMapping<TEntity> table, CancellationToken? token = null)
        {
            return table.ReadListAsync(reader, token);
        }

        /// <summary>
        /// Read list of entity from SqlDataReader and return it 
        /// </summary>
        /// <typeparam name="TEntity"> Entity type  </typeparam>
        /// <param name="reader"> Sql Data Reader </param>
        /// <param name="table"> Table mapping from sql to entity </param>
        /// <param name="token"></param>
        /// <returns> Entity instance or null </returns>
        public static async Task<List<TEntity>> ReadListAsync<TEntity>(this Task<DbDataReader> reader, ITableMapping<TEntity> table, CancellationToken? token = null)
        {
            return await ReadListAsync(await reader, table, token);
        }
    }
}

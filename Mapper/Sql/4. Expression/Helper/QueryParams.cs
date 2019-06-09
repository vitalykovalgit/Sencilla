using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    /// <summary>
    /// Parameter helper classs
    /// </summary>
    public class QueryParams
    {
        private int _lastParamIdx = 0;
        private Dictionary<string, object> _parameters = new Dictionary<string, object>();

        public QueryParams()
        {
        }

        public QueryParams(params object[] parameters)
        {
            AddRange(parameters);
        }

        public int Count => _parameters.Count;        

        /// <summary>
        /// Indexer 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string this[object obj] => Add(obj);

        /// <summary>
        /// Returns the param name for this value 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Add(object value)
        {
            var paramName = $"@p{_lastParamIdx++}";
            _parameters.Add(paramName, value);

            return paramName;
        }

        public string AddAsArray<T>(T[] values)
        {
            var paramNames = new List<string>();

            foreach (var value in values)
            {
                paramNames.Add(this.Add(value));
            }

            return string.Join(", ", paramNames);
        }

        public QueryParams AddRange(params object[] parameters)
        {
            foreach (var parameter in parameters)
                Add(parameter);

            return this;
        }

        public DbParameter[] ToArray(IDbProviderParam factory)
        {
            return _parameters.Select(kvp => factory.Create(kvp.Key, kvp.Value)).ToArray();
        }
    }
}

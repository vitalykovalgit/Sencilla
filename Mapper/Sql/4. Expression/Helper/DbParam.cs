using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    struct ParamTypes
    {
        public const string TInclude = "TInclude";
        public const string TId = "TId";
    }

    /// <summary>
    /// Wrapper class to simplify work with System.Data.Common.DbParameter
    /// </summary>
    public class DbParam
    {
        struct Params
        {
            public const string Id = "id";
            public const string PageId = "pageId";
            public const string Name = "name";
            public const string Ids = "ids";
            public const string PageType = "pageType";
            public const string Includes = "includes";
            public const string StorageType = "storageType";
            public const string Login = "login";
        }

        private readonly Func<IDbProviderParam, DbParam, DbParameter> ConvertToParameter;

        /// <summary>
        /// Parameter name
        /// </summary>
        public string ParamName { get; }

        /// <summary>
        /// Parameter value
        /// </summary>
        public object ParamValue { get; }

        /// <summary>
        /// Parameter .NET type.
        /// </summary>
        public Type ParamType { get; }

        #region Instance constructors

        public DbParam(string name, object value)
        {
            ParamName = name;
            ParamValue = value;
        }

        public DbParam(string name, object value, Type type)
        {
            ParamName = name;
            ParamValue = value;
            ParamType = type;
        }

        private DbParam(string name, object value, Func<IDbProviderParam, DbParam, DbParameter> convert) 
        {
            ParamName = name;
            ParamValue = value;
            ConvertToParameter = convert;
        }

        #endregion

        /// <summary>
        /// Convert instance to System.Data.Common.DbParameter with <paramref name="factory"/>
        /// </summary>
        /// <param name="factory">Factory to convert with</param>
        public DbParameter ToParameter(IDbProviderParam factory)
        {
            if (ConvertToParameter != null) return ConvertToParameter(factory, this);

            if (ParamType != null) return factory.Create(ParamName, ParamValue, ParamType);

            return factory.Create(ParamName, ParamValue);
        }

        #region Constructor methods

        /// <summary>
        /// Create param with name 'id'
        /// </summary>
        public static DbParam Id(object value)
        {
            return new DbParam(Params.Id, value);
        }

        /// <summary>
        /// Create param with name 'PageId'
        /// </summary>
        public static DbParam PageId(int value)
        {
            return new DbParam(Params.PageId, value);
        }

        /// <summary>
        /// Create param with name 'pageType'
        /// </summary>
        public static DbParam PageType(int value)
        {
            return new DbParam(Params.PageType, value);
        }

        /// <summary>
        /// Create param with name 'name'
        /// </summary>
        public static DbParam Name(string value)
        {
            return new DbParam(Params.Name, value);
        }

        /// <summary>
        /// Create param with name 'storageType'
        /// </summary>
        public static DbParam StorageType(int value)
        {
            return new DbParam(Params.StorageType, value);
        }

        /// <summary>
        /// Create param with name 'login'
        /// </summary>
        public static DbParam Login(string value)
        {
            return new DbParam(Params.Login, value);
        }

        /// <summary>
        /// Create param with name 'includes'
        /// </summary>
        /// <param name="includes">List of includes as parameter value</param>
        public static DbParam Includes(IList<Include> includes)
        {
            return new DbParam(Params.Includes, new IncludeTable(includes), (factory, param) =>
            {
                var p = factory.CreateStructured(ParamTypes.TInclude);
                p.ParameterName = param.ParamName;
                p.Value = param.ParamValue;

                return p;
            });
        }

        public static DbParam IncludesEmpty()
        {
            return Includes(new List<Include>());
        }

        /// <summary>
        /// Create param with name 'ids'
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static DbParam Ids(object[] values)
        {
            return new DbParam(Params.Ids, new IdTable(values), (factory, param) =>
            {
                var p = factory.CreateStructured(ParamTypes.TId);
                p.ParameterName = param.ParamName;
                p.Value = param.ParamValue;

                return p;
            });
        }

        /// <summary>
        /// Create param TId param with name <paramref name="paramName"/>
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static DbParam TId(string paramName, object[] values)
        {
            return new DbParam(paramName, new IdTable(values), (factory, param) =>
            {
                var p = factory.CreateStructured(ParamTypes.TId);
                p.ParameterName = param.ParamName;
                p.Value = param.ParamValue;

                return p;
            });
        }

        /// <summary>
        /// Create param TInclude with name <paramref name="paramName"/>
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="includes">List of includes as parameter value</param>
        public static DbParam TInclude(string paramName, IList<Include> includes)
        {
            return new DbParam(paramName, new IncludeTable(includes), (factory, param) =>
            {
                var p = factory.CreateStructured(ParamTypes.TInclude);
                p.ParameterName = param.ParamName;
                p.Value = param.ParamValue;

                return p;
            });
        }

        #endregion
    }
}

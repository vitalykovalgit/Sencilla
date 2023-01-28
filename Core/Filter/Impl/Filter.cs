
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class Filter<TEntity> : Filter where TEntity : class, IBaseEntity
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class Filter : IFilter
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }

        public string? Search { get; set; }

        public string[]? OrderBy { get; set; }
        public bool? Descending { get; set; }

        public string[]? With { get; set; }

        public IDictionary<string, FilterProperty>? Properties { get; set; }

        public void AddProperty(string name, Type? type, params object[] values)
        {
            Properties ??= new Dictionary<string, FilterProperty>();
            
            if (!Properties.ContainsKey(name))
                Properties[name] = new FilterProperty { Query = name, Type = type };

            Properties[name].AddValues(values);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<object>? GetProperty(string name)
        {
            return Properties?[name].Values;
        }
    }

}

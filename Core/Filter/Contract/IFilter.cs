
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// How much values to take
        /// </summary>
        int? Skip { get; set; }

        /// <summary>
        /// How much values to take
        /// </summary>
		int? Take { get; set; }

        /// <summary>
        /// Column name by wich we need to order 
        /// </summary>
        string[]? OrderBy { get; set; }

        /// <summary>
        /// Order direction 
        /// </summary>
        bool? Descending { get; set; }

        /// <summary>
        /// Retrieve entity with navigation property
        /// </summary>
        public string[]? With { get; set; }

        /// <summary>
        /// Search by any fields that is varchar 
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Contains list of properties by which we need to make select 
        /// </summary>
        IDictionary<string, IEnumerable<object>>? Properties { get; }

        /// <summary>
        /// Add property by which user wants to filter entity 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        void AddProperty(string name, params object[] values);

    }

}

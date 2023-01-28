
namespace Sencilla.Core
{
    /// <summary>
    /// Filter property
    /// </summary>
    public class FilterProperty
    {
        /// <summary>
        /// 
        /// </summary>
        public Type? Type { get; set; }

        /// <summary>
        /// Query or property name 
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<object>? Values {get; set;}

        //public List<>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        public void AddValues(params object[] values)
        {
            Values ??= new List<object>();
            Values.AddRange(values);
        }
    }

}

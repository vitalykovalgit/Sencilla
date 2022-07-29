
namespace Sencilla.Core
{
    public class Filter : IFilter
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }

        public string? Search { get; set; }

        public string[]? OrderBy { get; set; }
        public bool? Descending { get; set; }

        public string[]? With { get; set; }

        public IDictionary<string, IEnumerable<object>>? Properties { get; set; }

        public void AddProperty(string name, params object[] values)
        {
            var props = Properties ??= new Dictionary<string, IEnumerable<object>>();
            props[name] = values;
        }
    }

}

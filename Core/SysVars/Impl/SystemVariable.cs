
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    [Implement(typeof(ISystemVariable), PerRequest = true)]
    public class SystemVariable : ISystemVariable
    {
        Dictionary<string, object> Variables = new();

        /// <summary>
        /// Add/rewrite variable in system variables
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public void Set(string name, object data)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            Variables[name.ToLower()] = data;
        }

        /// <summary>
        /// The same as Get<T> where T is object?
        /// </summary>
        public object? Get(string? name) => Get<object?>(name);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">name of variable, case insencitive</param>
        /// <returns></returns>
        public T? Get<T>(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return default(T);

            var lname = name.ToLower();
            var obj = Variables.ContainsKey(lname) ? Variables[lname] : default(T);
            return (T?)obj;
        }
    }
}

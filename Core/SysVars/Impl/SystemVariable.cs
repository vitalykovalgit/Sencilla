
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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public void Add(string name, object data)
        {
            Variables.Add(name, data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object? Get(string name)
        { 
            return Variables.ContainsKey(name) ? null : Variables[name];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T? Get<T>(string name)
        {
            var obj = Variables.ContainsKey(name) ? null : Variables[name];
            return (T?)obj;
        }
    }
}

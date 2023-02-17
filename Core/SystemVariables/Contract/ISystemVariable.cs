
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISystemVariable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        void Set(string name, object? data);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object? Get(string? name);
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        T? Get<T>(string? name);
    }
}

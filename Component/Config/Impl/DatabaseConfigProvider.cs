
using Sencilla.Component.Config.Contract;

namespace Sencilla.Component.Config.Impl
{
    /// <summary>
    /// Retrieve configuration from DB 
    /// </summary>
    public class DatabaseConfigProvider<TConfig> : IConfigProvider<TConfig> where TConfig : class
    {
        public TConfig GetConfig()
        {
            throw new System.NotImplementedException();
        }
    }
}

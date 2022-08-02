
namespace Sencilla.Component.Config
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

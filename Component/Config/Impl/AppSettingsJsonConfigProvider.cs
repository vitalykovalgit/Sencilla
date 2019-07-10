using Microsoft.Extensions.Configuration;
using Sencilla.Component.Config.Contract;

namespace Sencilla.Component.Config.Impl
{
    /// <summary>
    /// Provide config from appsettings.json file 
    /// </summary>
    /// <typeparam name="TConfig"></typeparam>
    public class AppSettingsJsonConfigProvider<TConfig> : IConfigProvider<TConfig> where TConfig : class, new()
    {
        IConfiguration Config;
        public AppSettingsJsonConfigProvider(IConfiguration config)
        {
            Config = config;
        }

        public TConfig GetConfig()
        {
            var config = new TConfig();
            Config.GetSection(typeof(TConfig).Name).Bind(config);
            return config;
        }
    }
}

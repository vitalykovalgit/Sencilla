
namespace Sencilla.Component.Config;

/// <summary>
/// Provide config from appsettings.json file 
/// </summary>
/// <typeparam name="TConfig"></typeparam>
public class AppSettingsJsonConfigProvider<TConfig>(IConfiguration config) : IConfigProvider<TConfig> where TConfig : class, new()
{
    public TConfig GetConfig()
    {
        var options = new TConfig();
        config.GetSection(typeof(TConfig).Name).Bind(options);
        return options;
    }
}

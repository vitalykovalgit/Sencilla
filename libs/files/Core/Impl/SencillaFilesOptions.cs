namespace Sencilla.Component.Files;

/// <summary>
/// 
/// </summary>
public abstract class BaseFilesOptions
{
    /// <summary>
    /// 
    /// </summary>
    public abstract byte Type { get; }

    /// <summary>
    /// 
    /// </summary>
    public abstract string Section { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool UseAsDefault { get; set; }

    /// <summary>
    /// Predefined directories that contains mapping
    /// type -> directory
    /// </summary>
    public Dictionary<string, string> Dirs { get; set; } = [];

    /// <summary>
    /// Allow-list of accepted upload MIME types. Empty (default) = allow all,
    /// preserving backward compatibility for consumers that never configure it.
    /// </summary>
    public HashSet<string> AllowedMimeTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="params"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string GetDirectory(string type, params object[] @params)
    {
        Dirs.TryGetValue(type.Trim(), out var dir);
        if (dir == null)
            throw new Exception($"Could not find configuration for type={type} in {GetType().FullName} options");

        return string.Format(dir.Trim(), @params);
    }
}

/// <summary>
/// 
/// </summary>
[DisableInjection]
public class SencillaFilesOptions(IServiceCollection services): BaseFilesOptions
{
    
    public IServiceCollection Services { get; private set; } = services;

    public override byte Type => 0;
    public override string Section => "SencillaFiles";

    
    public SencillaFilesOptions AddProvider()
    {
        //
        return this;
    }

    /// <summary>
    /// Clean up the builder 
    /// </summary>
    public void Done()
    {
        Services = null!;
    }
    
}

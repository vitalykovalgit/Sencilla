namespace Sencilla.Component.Files;

/// <summary>
/// 
/// </summary>
public abstract class BaseFilesOptions
{
    /// <summary>
    /// Storage instance id. Persisted in <c>File.Storage</c> and used as the DI service key,
    /// so the same byte must mean the same storage in every environment — a database restored
    /// into another environment resolves its files by this number.
    /// Defaults to the provider's historical id (1 local drive, 2 Azure, 3 S3); set it in
    /// configuration to run several instances of one provider side by side, e.g. two S3
    /// buckets on different endpoints.
    /// </summary>
    public virtual byte Type { get; set; }

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

    public override byte Type { get; set; }
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


namespace Sencilla.Web;

/// <summary>
/// Generate Crud Api controller for entity 
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CrudApiAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="route"></param>
    public CrudApiAttribute(string? route = null, bool cache = false)
    {
        Route = route;
        Cache = cache;
    }

    public string? Route { get; }
    public bool Cache { get; }
}

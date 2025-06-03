
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
    public CrudApiAttribute(string? route = null)
    {
        Route = route;
    }

    public string? Route { get; }
}

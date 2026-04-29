
namespace Sencilla.Core;

/// <summary>
/// Filter property
/// </summary>
public class FilterProperty
{
    /// <summary>
    /// 
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Query or property name 
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<object?>? Values {get; set;}

    /// <summary>
    /// When true the property stores a JSON array string (e.g. "[1,2,3]").
    /// </summary>
    public bool IsJsonArray { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    public void AddValues(params object?[] values)
    {
        Values ??= [];
        Values.AddRange(values);
    }
}

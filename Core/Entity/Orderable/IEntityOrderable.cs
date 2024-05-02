
namespace Sencilla.Core;

/// <summary>
/// This interface is used to allow sorting of entity 
/// </summary>
public interface IEntityOrderable<TKey>: IBaseEntity
{   
    /// <summary>
    /// Order of the entity 
    /// </summary>
    TKey Order { get; set; }
}

/// <summary>
/// 
/// </summary>
public interface IEntityOrderable : IEntityOrderable<int>
{
}

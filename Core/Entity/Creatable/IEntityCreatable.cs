
namespace Sencilla.Core;

/// <summary>
/// Used to check if entity is creatable 
/// without generic parameters
/// </summary>
public interface IEntityCreateable: IBaseEntity
{
}

/// <summary>
/// 
/// </summary>
public interface IEntityCreateableTrack: IEntityCreateable
{
    DateTime CreatedDate { get; set; }
}

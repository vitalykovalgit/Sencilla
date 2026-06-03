
namespace Sencilla.Core;

/// <summary>
/// Sometimes need to check if a class is an entity, 
/// using generic class is very inconvinient for this 
/// </summary>
public interface IBaseEntity
{
}

/// <summary>
/// Base interface for all the entity 
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IEntity<TKey> : IBaseEntity
{
    /// <summary>
    /// Every entity must have unique identifier 
    /// </summary>
    TKey Id { get; set; }
}

/// <summary>
/// Use int as default parameter for generic argument
/// </summary>
public interface IEntity : IEntity<int>
{
}

/// <summary>
/// Marks an entity with a globally unique identifier for cross-service and cross-database identity.
/// The INT primary key remains the internal join key; GlobalId is the only identity
/// exposed in API responses, JWT tokens, and event payloads.
/// </summary>
public interface IEntityGlobal
{
    Guid GlobalId { get; set; }
}

/// <summary>
/// Dual of <see cref="IEntityGlobal"/> for entities whose primary key is a GUID
/// (<see cref="IEntity{TKey}"/> with TKey = <see cref="Guid"/>). The GUID Id is the internal,
/// technical identity; DisplayId is a compact, human-facing sequential number (INT IDENTITY)
/// shown in emails, references, and UI. Store-generated — never set by the client.
/// </summary>
public interface IEntityDisplay
{
    int DisplayId { get; set; }
}

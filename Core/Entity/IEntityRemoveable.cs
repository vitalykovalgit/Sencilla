using System;
using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// Item can be marked in DB as deleted 
    /// </summary>
    public interface IEntityRemoveable : IEntity
    {
        DateTime? DeletedDate { get; set; }
    }

    /// <summary>
    /// Item can be marked in DB as deleted 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IEntityRemoveable<TKey> : IEntity<TKey>, IEntityRemoveable
    {
    }
}

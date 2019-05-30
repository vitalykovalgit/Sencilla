using System;
using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// Interface marker is intentional here 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IEntityCreateable : IEntity
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public interface IEntityCreateable<TKey> : IEntity<TKey>, IEntityCreateable
    {
        DateTime CreatedDate { get; set; }
    }
}

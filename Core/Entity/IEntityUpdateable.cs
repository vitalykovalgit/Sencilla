using System;
using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// Allow entity to be updatable 
    /// </summary>
    public interface IEntityUpdateable : IEntity
    {
        /// <summary>
        /// Last updated date time 
        /// </summary>
        DateTime UpdatedDate { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
	public interface IEntityUpdateable<TKey> : IEntity<TKey>, IEntityUpdateable
    {
    }
}

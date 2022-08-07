
namespace Sencilla.Core
{
    /// <summary>
    /// Item can be marked in DB as deleted 
    /// </summary>
    public interface IEntityRemoveable : IEntityUpdateable
    {
        DateTime? DeletedDate { get; set; }
    }

}

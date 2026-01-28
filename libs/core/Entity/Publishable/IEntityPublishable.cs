namespace Sencilla.Core;

/// <summary>
/// Item can be marked in DB as deleted 
/// </summary>
public interface IEntityPublishable<TKey>: IEntity<TKey>, IEntityUpdateable
{
    byte PublishStatus { get; set; }
    string? PublishService { get; set; }
    DateTime? PublishStartDate { get; set; }
    DateTime? PublishFinishDate { get; set; }

    DateTime CreatedDate { get; set; }

    byte[] RowVersion { get; set; }
}


public interface IEntityPublishable: IEntityPublishable<int>
{ 
}


namespace Sencilla.Core;

/// <summary>
/// Item can be marked in DB as deleted 
/// </summary>
public interface IEntityPublishable: IEntityUpdateable
{
    public byte PublishStatus { get; set; }
    public string? PublishService { get; set; }
    public DateTime? PublishStartDate { get; set; }
    public DateTime? PublishFinishDate { get; set; }

    public byte[] RawVersion { get; set; }
}

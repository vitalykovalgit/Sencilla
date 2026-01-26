namespace Sencilla.Component.Files;


[Table(nameof(File))]
[MainEntity(typeof(File))]
public class FileUpload: IEntity<Guid>, IEntityUpdateable, IEntityDeleteable
{
    /// <summary>
    /// References to File.Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    //public long Size { get; set; }

    /// <summary>
    /// Current byte position
    /// </summary>
    public long Uploaded { get; set; }
}

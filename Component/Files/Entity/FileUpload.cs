namespace Sencilla.Component.Files;

public class FileUpload
    : IEntity<Guid>
    , IEntityCreateable
    , IEntityUpdateable
    , IEntityDeleteable
{
    /// <summary>
    /// References to File.Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Current byte position
    /// </summary>
    public long Position { get; set; }

    /// <summary>
    /// True if file upload completed (Size == Position)
    /// </summary>
    public bool UploadCompleted { get; set; }

    public string? Attrs { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }
}

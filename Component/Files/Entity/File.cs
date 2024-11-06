namespace Sencilla.Component.Files;

public class File
    : IEntity<Guid>
    , IEntityCreateable
    , IEntityUpdateable
    , IEntityRemoveable
    , IEntityDeleteable
{
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
    /// File name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Full file name with path
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Mime type
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// File extension with dot: '.jpg', '.png'
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// Storage file type
    /// </summary>
    public long? StorageFileTypeId { get; set; }

    /// <summary>
    /// True if file upload completed (Size == Position)
    /// </summary>
    public bool UploadCompleted { get; set; }

    public string? Attrs { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }
}

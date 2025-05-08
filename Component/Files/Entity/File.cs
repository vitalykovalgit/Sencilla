namespace Sencilla.Component.Files;

[CrudApi("api/v1/files")]
public class File
    : IEntity<Guid>
    , IEntityCreateable
    , IEntityUpdateable
    , IEntityRemoveable
    , IEntityDeleteable
{
    public Guid Id { get; set; }

    /// <summary>
    /// Original file
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Dimensions 
    /// </summary>
    public int? Dim { get; set; }

    /// <summary>
    /// Full file name with path
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Mime type
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// File extension with dot: '.jpg', '.png'
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// User
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Where is this file from (System, User upload)
    /// </summary>
    public FileOrigin Origin { get; set; } = FileOrigin.None;

    /// <summary>
    /// Storage file type
    /// </summary>
    public FileContentProviderType StorageFileTypeId { get; set; }

    /// <summary>
    /// Image width dimension
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image height dimension
    /// </summary>
    public int? Height { get; set; }

    public string? Attrs { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }
}

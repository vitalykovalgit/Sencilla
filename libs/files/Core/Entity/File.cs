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
    public string Name { get; set; } = default!;


    /// <summary>
    /// Mime type
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The uploaded size 
    /// </summary>
    public long Uploaded { get; set; }

    /// <summary>
    /// User
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Where is this file from (System, User upload)
    /// </summary>
    public FileOrigin Origin { get; set; } = FileOrigin.None;

    /// <summary>
    /// Path
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Storage file type
    /// </summary>
    public byte Storage { get; set; }

    /// <summary>
    /// Dimensions 
    /// TODO: Think if we can remove this fields or move to attribute??
    /// </summary>
    public int? Dim { get; set; }

    /// <summary>
    /// Image width dimension
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image height dimension
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Attrs { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }
}

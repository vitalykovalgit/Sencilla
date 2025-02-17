namespace Sencilla.Component.Files;

//[CrudApi("api/v1/files")]
//[CrudApi("api/v1/files/content")]
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
    public Guid? OriginalFileId { get; set; }

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
    public string? MimeType { get; set; }

    /// <summary>
    /// File extension with dot: '.jpg', '.png'
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// Where is this file from (System, User upload)
    /// </summary>
    public FileOrigin Origin { get; set; } = FileOrigin.None;

    public string? Attrs { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }
}

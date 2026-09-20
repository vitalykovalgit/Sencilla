namespace Sencilla.Component.Files;

/// <summary>
/// Projection mapped to the File table for efficient Size-only updates.
///
/// Deliberately separate from <see cref="FileUpload"/>: that one carries Uploaded, and a projection
/// holding both would zero whichever column a caller left out. Size is what HEAD answers as
/// Upload-Length, so losing it silently breaks resume (see <see cref="CreateFileHandler"/>).
///
/// SECURITY: a projection is its OWN matrix resource. SecurityProvider.ResourceName reads the type
/// name and [MainEntity] is consumed only by DynamicDbContext, so this inherits nothing from the
/// `file` grants — a host with no `filesizeupdate` row gets a bare ForbiddenException on every
/// ORIGINAL upload while the resized variants still succeed. Grant it beside `fileupload`.
/// </summary>
[MainEntity(typeof(File))]
[Table(nameof(File))]
public class FileSizeUpdate : IEntity<Guid>, IEntityUpdateable
{
    public Guid Id { get; set; }

    /// <summary>
    /// Declared total file size in bytes (see <see cref="File.Size"/>).
    /// </summary>
    public long Size { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Sencilla.Component.Files;

public class File
    : IEntity<ulong>
    , IEntityCreateable
    , IEntityUpdateable
    , IEntityRemoveable
    , IEntityDeleteable
{
    public ulong Id { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File name 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Path to file on disk 
    /// </summary>
    //public string Path { get; set; }

    /// <summary>
    /// Mime type 
    /// </summary>
    public string MimeType { get; set; }

    public string Extension { get; set; }

    /// <summary>
    /// Storage file type 
    /// </summary>
    public long? StorageFileTypeId { get; set; }

    public bool UploadCompleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }


    [ForeignKey(nameof(Id))]
    public FileContent FileContent { get; set; }

    public File SetContent(byte[] content)
    {
        Size = content.Length;
        FileContent.Content = content;
        return this;
    }

    public byte[] Content()
    {
        return FileContent?.Content ?? new byte[] { };
    }
}

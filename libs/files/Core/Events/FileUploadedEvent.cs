namespace Sencilla.Component.Files;

[DisableInjection]
public class FileUploadedEvent : Event
{
    public required File File { get; set; }

    /// <summary>
    /// The resolution that was uploaded (null for regular file uploads)
    /// </summary>
    public int? Resolution { get; set; }
}

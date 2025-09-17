namespace Sencilla.Component.Files;

[DisableInjection]
public class FileUploadedEvent : Event
{
    public File? File { get; set; }
    public FileUpload? FileUpload { get; set; }
}

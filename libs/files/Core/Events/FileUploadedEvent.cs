namespace Sencilla.Component.Files;

[DisableInjection]
public class FileUploadedEvent : Event
{
    public required File File { get; set; }
}

namespace Sencilla.Component.Files;

[DisableInjection]
public class FileDeletedEvent : Event
{
    public File? File { get; set; }
}

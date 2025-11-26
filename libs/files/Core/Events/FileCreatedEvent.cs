namespace Sencilla.Component.Files;

[DisableInjection]
public class FileCreatedEvent : Event
{
    public File? File { get; set; }
}

namespace Sencilla.Component.Files;

public class FileCreatedEvent : Event
{
    public File? File { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
}

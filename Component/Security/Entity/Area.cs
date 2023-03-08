
namespace Sencilla.Component.Security;

[Table(nameof(Area), Schema = "sec")]
public class Area : IEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
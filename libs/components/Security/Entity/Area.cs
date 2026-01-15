
using Sencilla.Web;

namespace Sencilla.Component.Security;

[UseCaching(60)]
[Table(nameof(Area), Schema = "sec")]
public class Area : IEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

namespace Sencilla.Component.Users;

[Table(nameof(Role), Schema = "sec")]
public class Role: IEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

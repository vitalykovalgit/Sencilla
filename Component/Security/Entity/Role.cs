
using Sencilla.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sencilla.Component.Security
{

    [Table(nameof(Role), Schema = "sec")]
    public class Role: IEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}

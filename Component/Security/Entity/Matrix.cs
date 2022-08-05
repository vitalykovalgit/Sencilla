using Sencilla.Core;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sencilla.Component.Security
{
    [Table(nameof(Matrix), Schema = "sec")]
    public class Matrix : IEntity
    {
        public int Id { get; set; }
        public int Role { get; set; }
        public string? Resource { get; set; }
        //public int Area { get; set; }
        public int Action { get; set; }

        public string? Constraint { get; set; }
    }
}

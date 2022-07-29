using Sencilla.Core;

namespace Sencilla.Component.Security.Entity
{
    public class Matrix : IEntity
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public string? Resource { get; set; }
        public int Area { get; set; }
        public int Actions { get; set; }
    }
}

using Sencilla.Core;

namespace Sencilla.Component.Security.Entity
{
    public class Matrix : IEntity
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public string Entity { get; set; }
        public ulong Area { get; set; }
        public ulong Actions { get; set; }
    }
}

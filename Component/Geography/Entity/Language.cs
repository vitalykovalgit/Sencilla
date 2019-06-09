
using Sencilla.Core.Entity;

namespace Sencilla.Component.Geography.Entity
{
    public class Language : IEntity<uint>
    {
        public uint Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }
    }
}

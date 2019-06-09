
using Sencilla.Core.Entity;

namespace Sencilla.Component.Geography.Entity
{
    public class Country : IEntity<uint>
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Iso2 { get; set; }
        public string Iso3 { get; set; }
        public string Code { get; set; }
        public string PhoneCode { get; set; }
    }
}


using Sencilla.Core;

namespace Sencilla.Component.Geography
{
    public class Country : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Iso2 { get; set; }
        public string Iso3 { get; set; }
        public string Code { get; set; }
        public string PhoneCode { get; set; }
    }
}

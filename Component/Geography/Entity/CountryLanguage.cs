
using Sencilla.Core.Entity;

namespace Sencilla.Component.Geography.Entity
{
    public class CountryLanguage : IEntity<uint>
    {
        public uint Id { get; set; }
        public uint CountryId { get; set; }
        public uint LanguageId { get; set; }
    }
}

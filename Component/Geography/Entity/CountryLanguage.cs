
using Sencilla.Core;

namespace Sencilla.Component.Geography
{
    public class CountryLanguage : IEntity
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public int LanguageId { get; set; }
    }
}

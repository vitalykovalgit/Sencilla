
using Sencilla.Core;

namespace Sencilla.Component.Localization
{
    /// <summary>
    /// 
    /// </summary>
    public class Translate : IEntity
    {
        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Language identifier, references to Geography component Language entity
        /// </summary>
        public uint Language { get; set; }

        /// <summary>
        /// If no translation found default one will be taken 
        /// </summary>
        public bool Default { get; set; }

        /// <summary>
        /// Module identifier which is used to group translation for specific language 
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Unique identifier of string 
        /// </summary>
        public string StringId { get; set; }
        
        /// <summary>
        /// Value of the translated string 
        /// </summary>
        public string Value { get; set; }
    }
}

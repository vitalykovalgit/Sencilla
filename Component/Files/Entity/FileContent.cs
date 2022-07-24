using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sencilla.Core;

namespace Sencilla.Component.Files
{
    /// <summary>
    /// 
    /// </summary>
    [Table("Content", Schema = "file")]
    public class FileContent : IEntity<long>
    {
        /// <summary>
        /// Must be inserted manually 
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        /// <summary>
        /// Content of the file
        /// </summary>
        public byte[] Content { get; set; }

    }
}

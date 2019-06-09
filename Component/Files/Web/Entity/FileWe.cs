using System;

using Sencilla.Component.Files.Entity;
using Sencilla.Core.Web;

namespace Sencilla.Component.Files.Web.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public class FileWe : IWebEntity<File, ulong>
    {
        public FileWe() 
        {
        }

        public FileWe(File file)
        {
            FromEntity(file);
        }

        public ulong Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
		public long? Size { get; set; }

        /// <summary>
        /// File name 
        /// </summary>
	    public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        public void FromEntity(File entity)
        {
            Id = entity.Id;
            Size = entity.Size;
            Name = entity.Name;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
        }

        public File ToEntity(File dbEntity)
        {
            dbEntity.Name = Name ?? dbEntity.Name;
            dbEntity.Size = Size ?? dbEntity.Size;
            return dbEntity;
        }
    }
}

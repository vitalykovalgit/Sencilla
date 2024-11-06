using System;
using Sencilla.Core;

namespace Sencilla.Component.Files
{
    /// <summary>
    /// 
    /// </summary>
    public class FileWe : IDtoEntity<File, Guid>
    {
        public FileWe() 
        {
        }

        public FileWe(File file)
        {
            FromEntity(file);
        }

        public Guid Id { get; set; }

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

using Sencilla.Core;

namespace Sencilla.Component.Users
{
    public class User 
      : IEntityCreateable
      , IEntityUpdateable
      , IEntityRemoveable
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FatherName { get; set; }
        public string? Email { get; set; }
        public ulong Phone { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}

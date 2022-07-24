using System;
using Sencilla.Core;

namespace Sencilla.Component.Users
{
    public class User 
      : IEntityCreateable<ulong>
      , IEntityUpdateable<ulong>
      , IEntityRemoveable<ulong>
    {
        public ulong Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public ulong Phone { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}

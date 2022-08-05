using Sencilla.Core;
using System.ComponentModel.DataAnnotations.Schema;

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

        //[ForeignKey(nameof(UserRole.UserId))]
        public ICollection<UserRole>? Roles { get; set; }

        //[ForeignKey(nameof(UserAttribute.UserId))]
        public ICollection<UserAttribute>? Attributes { get; set; }

        public User AddRole(int role) 
        {
            var roles = Roles ??= new List<UserRole>();
            roles.Add(new UserRole
            {
                UserId = Id,
                RoleId = role
                //Role = role.ToString(),
            });

            return this;
        }
    }
}

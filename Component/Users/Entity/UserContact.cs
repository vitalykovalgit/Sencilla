using Sencilla.Core;

namespace Sencilla.Component.Users
{
    public class UserContact: IEntity, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string? Address { get; set; }
        public string? Building { get; set; }
        public string? Apartment { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

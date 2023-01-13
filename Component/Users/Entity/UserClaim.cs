using Sencilla.Core;

namespace Sencilla.Component.Users
{
    public class UserClaim: IEntity, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        public string? Name { get; set; }
        public string? Value { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

using Sencilla.Core;

namespace Sencilla.Component.Security.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public class UserRole 
        : IEntityCreateable
        , IEntityUpdateable
        , IEntityDeleteable
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Role { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

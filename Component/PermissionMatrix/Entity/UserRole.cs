using System;
using Sencilla.Core;

namespace Sencilla.Component.Security.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public class UserRole 
        : IEntityCreateable<ulong>
        , IEntityDeleteable<ulong>
    {
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public string Role { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}

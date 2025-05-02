
namespace Sencilla.Repository.EntityFramework.Tests
{
    public class Entity: IEntity, IEntityCreateableTrack
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = default!;

        public bool IsRegistered { get; set; }
        
        public int Status { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
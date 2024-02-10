
namespace Sencilla.Repository.EntityFramework.Registrator;
public class EntityTypeDetails
{
    public EntityTypeDetails(Type entityType, Type? splitedType, params string[] commonProperties)
    {
        Main = entityType;
        Splited = splitedType;
        CommonProperties = commonProperties.ToList();
    }

    public Type Main { get; }

    public Type? Splited { get; }

    public IEnumerable<string> CommonProperties { get; }
}

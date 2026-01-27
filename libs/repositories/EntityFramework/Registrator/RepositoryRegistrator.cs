namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
public class RepositoryRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        // Add entity to collection 
        container.RegisterEFRepositoriesForType(type, out bool isAdded);
        if (isAdded && !RepositoryEntityFrameworkBootstrap.Entities.Contains(type))
        {
            RepositoryEntityFrameworkBootstrap.Entities.Add(type);
        }
    }
}

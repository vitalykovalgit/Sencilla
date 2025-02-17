
namespace Sencilla.Core
{
    /// <summary>
    /// Register type in container 
    /// </summary>
    public interface IContainer
    {
        IContainer RegisterInstance(Type service, object instance);
        IContainer RegisterInstance<TService>(TService instance) where TService: class;

        IContainer RegisterType(Type service, string? name = null);
        IContainer RegisterTypeSingleton(Type service, string? name = null);
        IContainer RegisterTypePerRequest(Type service, string? name = null);

        IContainer RegisterType(Type service, Type implementation, string? name = null);
        IContainer RegisterTypeSingleton(Type service, Type implementation, string? name = null);
        IContainer RegisterTypePerRequest(Type service, Type implementation, string? name = null);

        IContainer RegisterType<TService>(string? name = null) where TService: class;
        IContainer RegisterType<TService>(Func<IResolver, TService> implementationFactory) where TService : class;
        IContainer RegisterTypeSingleton<TService>(string? name = null) where TService: class;
        IContainer RegisterTypePerRequest<TService>(string? name = null) where TService: class;


        IContainer RegisterType<TService, TImplementation>(string? name = null) where TService : class where TImplementation : class, TService;
        IContainer RegisterTypeSingleton<TService, TImplementation>(string? name = null) where TService : class where TImplementation : class, TService;
        IContainer RegisterTypePerRequest<TService, TImplementation>(string? name = null) where TService : class where TImplementation : class, TService;
    }
}


namespace Sencilla.Core;

/// <summary>
/// Called during startup for every type in application 
/// Register type in container automatically 
/// Implement it if you need custom logic to automatically register 
/// your type 
/// </summary>
public interface ITypeRegistrator
{
    void Register(IServiceCollection container, Type type);
}

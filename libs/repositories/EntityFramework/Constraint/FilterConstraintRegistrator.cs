namespace Sencilla.Repository.EntityFramework;

public class FilterConstraintRegistrator : ITypeRegistrator
{
    /// <summary>
    /// Register command hadlers automatically 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="type"></param>
    public void Register(IServiceCollection container, Type type) => container.RegisterEFFilters(type);
}

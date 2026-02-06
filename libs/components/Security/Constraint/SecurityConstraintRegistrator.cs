namespace Sencilla.Component.Security;

public class SecurityConstraintRegistrator : ITypeRegistrator
{
    /// <summary>
    /// Register command hadlers automatically 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="type"></param>
    public void Register(IServiceCollection container, Type type) => container.AddSencillaSecurityFromDatabase(type);
}

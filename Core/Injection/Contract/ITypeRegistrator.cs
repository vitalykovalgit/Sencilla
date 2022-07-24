
namespace Sencilla.Core
{
    /// <summary>
    /// Register type in containers 
    /// Implement it if you need custom logic to register 
    /// your type 
    /// </summary>
    public interface ITypeRegistrator
    {
        void Register(IRegistrator container, Type type);
    }
}

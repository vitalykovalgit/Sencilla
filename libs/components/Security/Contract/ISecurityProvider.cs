
namespace Sencilla.Component.Security;

/// <summary>
/// Provide permissions 
/// </summary>
public interface ISecurityProvider
{
    IEnumerable<Matrix> Permissions<TEntity>(Action? action = null);
}




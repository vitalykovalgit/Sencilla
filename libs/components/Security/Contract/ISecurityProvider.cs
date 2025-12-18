
namespace Sencilla.Component.Security;

/// <summary>
/// Provide permissions 
/// </summary>
public interface ISecurityProvider
{
    Task<IEnumerable<Matrix>> Permissions<TEntity>(CancellationToken token, Action? action = null);
}




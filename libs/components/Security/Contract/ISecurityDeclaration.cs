
namespace Sencilla.Component.Security;

/// <summary>
/// Allows to define security permissions
/// </summary>
public interface ISecurityDeclaration
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<Matrix>> Permissions(CancellationToken token);
}




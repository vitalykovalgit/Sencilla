
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
    IEnumerable<Matrix> Permissions();
}





namespace Sencilla.Component.Users;

/// <summary>
/// Provide current user 
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// Retrieve current user 
    /// </summary>
    User CurrentUser { get; }

    /// <summary>
    /// Retrieve current principal 
    /// </summary>
    IPrincipal? CurrentPrincipal { get; }

}


namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
public static class SystemVariableEx
{
    const string UserVariable = "user";

    /// <summary>
    /// Retrieve current if it is exists in system variable 
    /// </summary>
    /// <param name="systemVars"></param>
    /// <returns></returns>
    public static User? GetCurrentUser(this ISystemVariable systemVars)
    {
        return systemVars.Get<User>(UserVariable);
    }

    /// <summary>
    /// Set current user to systen variable 
    /// </summary>
    /// <param name="systemVars"> System Variables </param>
    /// <param name="user"> Current User </param>
    /// <returns> Returns the passed instance of system variables </returns>
    public static ISystemVariable SetCurrentUser(this ISystemVariable systemVars, User user)
    {
        systemVars.Set(UserVariable, user);
        return systemVars;
    }

}

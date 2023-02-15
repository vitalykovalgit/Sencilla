
namespace Sencilla.Core;

public static class ObjectInjectEx
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="name"></param>
    /// <param name="params"></param>
    /// <returns></returns>
    public static async Task CallWithInjectAsync(this object obj, string name, params object[] @params)
    {
        var method = obj.GetType().GetMethod(name);
        await (Task)method.Invoke(obj, InjectParameters(method, @params));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="name"></param>
    /// <param name="params"></param>
    /// <returns></returns>
    public static async Task<T> CallWithInjectAsync<T>(this object obj, string name, params object[] @params)
    {
        var method = obj.GetType().GetMethod(name);
        return await (Task<T>)method.Invoke(obj, InjectParameters(method, @params));
    }

    private static object[] InjectParameters(MethodInfo method, params object[] @params)
    {
        // inject parameters but skip first parameter 
        var parameters = new List<object>(@params);
        var parameterTypes = method.GetParameters();
        for (var i = @params.Length; i < parameterTypes.Length; i++)
        {
            var service = ServiceLocator.R(parameterTypes[i].ParameterType);
            parameters.Add(service);
        }

        return parameters.ToArray();
    }
}

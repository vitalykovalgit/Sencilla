
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
        // get method matching parameters 
        var methods = obj.GetType().GetMethods().Where(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        var method = methods.FirstOrDefault(m => m.GetParameters().StartWith(@params, (f, s) => f.ParameterType == s.GetType()));

        // if method is empty use first one 
        method = method ?? methods.FirstOrDefault();
        await (Task)(method?.Invoke(obj, InjectParameters(method, @params)) ?? Task.CompletedTask);
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
        // get method matching parameters 
        var methods = obj.GetType().GetMethods().Where(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        var method = methods.FirstOrDefault(m => m.GetParameters().StartWith(@params, (f, s)=> f.ParameterType == s.GetType()));

        // if method is empty use first one 
        method = method ?? methods.FirstOrDefault();
        return await (Task<T>)(method?.Invoke(obj, InjectParameters(method, @params)) ?? Task.CompletedTask);
    }

    private static object?[] InjectParameters(MethodInfo method, params object[] @params)
    {
        // inject parameters but skip first parameter 
        var parameters = new List<object?>(@params);
        var parameterTypes = method.GetParameters();
        for (var i = @params.Length; i < parameterTypes.Length; i++)
        {
            var service = ServiceLocator.R(parameterTypes[i].ParameterType);
            parameters.Add(service);
        }

        return parameters.ToArray();
    }
}

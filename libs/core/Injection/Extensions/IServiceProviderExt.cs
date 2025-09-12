
namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceProviderExt
{
    public static Task InvokeMethod(this IServiceProvider provider, object? obj, string method, params object[] @params) 
    {
        if (provider == null || obj == null)
            return Task.CompletedTask;

        //var methodInfos = obj.GetType().GetMethods().Where(m => m.Name.Equals(method, StringComparison.OrdinalIgnoreCase));
        //var methodInfo = methodInfos.FirstOrDefault(m => m.GetParameters().StartWith(@params, (f, s) => f.ParameterType == s.GetType())) ?? methodInfos.FirstOrDefault();
        var methodInfo = obj.GetMethod(method, @params);
        return (Task)(methodInfo?.Invoke(obj, provider.InjectMethodParameters(methodInfo, @params)) ?? Task.CompletedTask);
    }

    public static async Task<T?> InvokeMethod<T>(this IServiceProvider provider, object? obj, string method, params object[] @params)
    {
        if (provider == null || obj == null)
            return default;

        //var methodInfos = obj.GetType().GetMethods().Where(m => m.Name.Equals(method, StringComparison.OrdinalIgnoreCase));
        //var methodInfo = methodInfos.FirstOrDefault(m => m.GetParameters().StartWith(@params, (f, s) => f.ParameterType == s.GetType())) ?? methodInfos.FirstOrDefault();
        var methodInfo = obj.GetMethod(method, @params);
        return await (Task<T>)(methodInfo?.Invoke(obj, provider.InjectMethodParameters(methodInfo, @params)) ?? Task.CompletedTask);
    }

    /// <summary>
    /// Let's try to find best method with most suitable parameters 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="params"></param>
    /// <returns></returns>
    public static MethodInfo? GetMethod(this object? obj, string method, object[] @params)
    { 
        if (obj == null) return null;
        
        // find all methods 
        var methodInfos = obj.GetType()
                             .GetMethods()
                             .Where(m => m.Name.Equals(method, StringComparison.OrdinalIgnoreCase));

        if (methodInfos.Count() == 0)
            return null;

        if (methodInfos.Count() == 1)
            return methodInfos.First();

        if (@params.Count() == 0)
            return methodInfos.FirstOrDefault();

        // Try to find method with most matched parameters 
        // Use same order as passed 
        var mmethdos = methodInfos; // matched methods 
        var mparams = new List<object>(); // matched params 
        foreach (var param in @params)
        {   
            mparams.Add(param);
            mmethdos = mmethdos.Where(m => m.GetParameters().StartWith(mparams, (l, r) => l.ParameterType == r.GetType()));
            if (mmethdos.Count() <= 1)
                break;
        }

        return mmethdos.FirstOrDefault() ?? methodInfos.FirstOrDefault();
    }

    public static object?[]? InjectMethodParameters(this IServiceProvider serviceProvider, MethodInfo method, params object[] @params)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return null;

        var parameterValues = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            // if parameter is already provided in @params use it 
            var param = parameters[i];
            var paramValues = @params.Where(p => p.GetType() == param.ParameterType);
            if (paramValues.Any())
            {
                parameterValues[i] = paramValues.First();
                continue;
            }

            var service = serviceProvider.GetService(param.ParameterType);
            if (service == null && !param.IsOptional)
                throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' of type '{param.ParameterType}' for method '{method.Name}'.");

            parameterValues[i] = service ?? param.DefaultValue;
        }

        return parameterValues;
    }
}

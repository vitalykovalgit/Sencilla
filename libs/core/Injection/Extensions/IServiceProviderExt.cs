
namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceProviderExt
{
    public static Task InvokeMethod(this IServiceProvider provider, object? obj, string method, params object[] @params) 
    {
        if (provider == null || obj == null)
            return Task.CompletedTask;
        
        var methodInfos = obj.GetType().GetMethods().Where(m => m.Name.Equals(method, StringComparison.OrdinalIgnoreCase));
        var methodInfo = methodInfos.FirstOrDefault(m => m.GetParameters().StartWith(@params, (f, s) => f.ParameterType == s.GetType())) ?? methodInfos.FirstOrDefault();
        
        return (Task)(methodInfo?.Invoke(obj, provider.InjectMethodParameters(methodInfo, @params)) ?? Task.CompletedTask);
    }

    public static async Task<T?> InvokeMethod<T>(this IServiceProvider provider, object? obj, string method, params object[] @params)
    {
        if (provider == null || obj == null)
            return default;

        var methodInfos = obj.GetType().GetMethods().Where(m => m.Name.Equals(method, StringComparison.OrdinalIgnoreCase));
        var methodInfo = methodInfos.FirstOrDefault(m => m.GetParameters().StartWith(@params, (f, s) => f.ParameterType == s.GetType())) ?? methodInfos.FirstOrDefault();

        return await (Task<T>)(methodInfo?.Invoke(obj, provider.InjectMethodParameters(methodInfo, @params)) ?? Task.CompletedTask);
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

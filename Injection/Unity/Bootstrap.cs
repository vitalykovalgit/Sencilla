using Sencilla.Core.Injection;
using Sencilla.Impl.Injection.Unity;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        public static void AddSencillaUnityInjection(this IServiceCollection services)
        {
            //
            services.AddSingleton<IResolver, UnityResolver>();
        }
    }
}


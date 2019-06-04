using Microsoft.Extensions.DependencyInjection;

using Sencilla.Core.Injection;
using Sencilla.Impl.Injection.Unity;

namespace Sencilla.Core.Builder
{
    public static class Bootstrap
    {
        public static IServiceCollection AddInjectionWithUnity(this IServiceCollection builder)
        {
            builder.AddSingleton<IResolver, UnityResolver>();
            return builder;
        }
    }
}


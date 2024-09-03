
global using Sencilla.Core;
global using Sencilla.Web;
global using System.Text.Json.Serialization;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        public static IServiceCollection AddSencillaWeb(this IServiceCollection builder, IMvcBuilder mvcBuilder)
        {
            mvcBuilder.AddMvcOptions(options =>
            {
                options.ModelBinderProviders.Insert(0, new FilterTypeBinderProvider(options.InputFormatters));
                options.Conventions.Add(new CrudApiControllerRouteConvention());
            })
            .ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Add(new CrudApiControllerFeatureProvider());
            })
            .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            return builder;
        }
    }
}

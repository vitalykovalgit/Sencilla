global using System.Net;
global using System.Reflection;
global using System.Text.Json.Serialization;

global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.ApplicationParts;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.AspNetCore.Mvc.ApplicationModels;
global using Microsoft.AspNetCore.Mvc.Authorization;
global using Microsoft.AspNetCore.Mvc.ModelBinding;
global using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
global using Microsoft.AspNetCore.Mvc.Formatters;
global using Microsoft.AspNetCore.Mvc.Infrastructure;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using Sencilla.Core;
global using Sencilla.Web;


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

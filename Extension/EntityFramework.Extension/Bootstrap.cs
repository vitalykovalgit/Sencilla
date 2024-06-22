global using System.Text;
global using System.Linq.Expressions;
global using System.Text.RegularExpressions;

global using Sencilla.Core;

global using System.Reflection;
global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations.Schema;
global using Sencilla.Repository.EntityFramework.Extension;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        public static IServiceCollection AddSencillaExtensionForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> action)
        {
            
            return builder;
        }
    }
}

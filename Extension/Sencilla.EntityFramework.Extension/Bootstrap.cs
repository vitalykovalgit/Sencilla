global using System;
global using System.Text;
global using System.Linq;
global using System.Linq.Expressions;

global using Sencilla.Core;

global using Microsoft.EntityFrameworkCore;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        public static IServiceCollection AddSencillaRepositoryForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> action)
        {
            
            return builder;
        }
    }
}

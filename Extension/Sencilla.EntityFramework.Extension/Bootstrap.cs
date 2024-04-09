global using System;
global using System.Data;
global using System.Collections;
global using System.Linq;
global using System.Linq.Expressions;

global using Sencilla.Core;

global using Microsoft.EntityFrameworkCore;

global using Microsoft.Data.SqlClient;
global using Sencilla.EntityFramework.Extension.Builder;
global using Sencilla.EntityFramework.Extension.Command;
global using Sencilla.EntityFramework.Extension.Contract;

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

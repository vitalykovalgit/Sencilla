global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Sencilla.Component.Users;
global using Sencilla.Core;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Diagnostics;
global using System.Linq.Dynamic.Core;
global using System.Linq.Expressions;
global using System.Reflection;
using Sencilla.Component.Security;
using System.ComponentModel;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// This method is used in startup class just to reference 
    /// this assambly with all it's component 
    /// Sencilla will register everything automatically
    /// </summary>
    public static IServiceCollection AddSencillaSecurity(this IServiceCollection builder)
    {
        // Try to add security for calling assembly
        //var assembly = new StackFrame(1).GetMethod()?.DeclaringType?.Assembly;
        //builder.AddSencillaSecurityForType(assembly);

        // Do nothing here
        return builder;
    }

    public static IServiceCollection AddSencillaSecurityForType(this IServiceCollection container, Assembly? assembly)
    { 
        if (assembly == null) 
            return container;

        foreach (var type in assembly.GetTypes())
        {
            container.AddSencillaSecurityFromDatabase(type);
            //container.AddSencillaSecurityFromAttributes(type, SecurityProvider.Permissions);
        }        

        // Do nothing here
        return container;
    }

    public static IServiceCollection AddSencillaSecurityFromAttributes(this IServiceCollection container, Type type, List<Matrix> permissions)
    {
        if (typeof(IBaseEntity).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
        {
            var attributes = type.GetCustomAttributes(typeof(AllowAccessAttribute), true);
            foreach (AllowAccessAttribute a in attributes)
            {
                // get resource 
                permissions.Add(new Matrix
                {
                    Resource = SecurityProvider.ResourceName(type),
                    Action = (int)a.Action,
                    Constraint = a.Constraint,
                    Role = a.Role,
                });
            }
        }

        // Do nothing here
        return container;
    }

    public static IServiceCollection AddSencillaSecurityFromDatabase(this IServiceCollection container, Type type)
    {
        if (type.IsAssignableTo(typeof(IBaseEntity)) && type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            var constraint = typeof(SecurityConstraintHandler<>).MakeGenericType(type);

            // read interafce 
            var readInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityReadingEvent<>).MakeGenericType(type));
            container.AddTransient(readInterface, constraint);

            // create interafce 
            var createInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatingEvent<>).MakeGenericType(type));
            container.AddTransient(createInterface, constraint);
        }

        // Do nothing here
        return container;
    }


}

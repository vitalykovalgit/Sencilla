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

    /// <summary>
    /// Enables admin impersonation: an operator holding <see cref="ImpersonationOptions.Resource"/> may
    /// browse as another user, with the real actor preserved for audit. Then chain
    /// <c>app.UseSencillaImpersonation()</c> after <c>UseSencillaUser()</c>.
    ///
    /// The cookie attributes are the host's to set — they must match the auth cookie's scoping exactly,
    /// or the two travel on different requests. The per-request <see cref="IImpersonationContext"/> is
    /// registered by discovery ([PerRequestLifetime]); TryAdd here keeps hosts that skip the scan working.
    /// </summary>
    public static IServiceCollection AddSencillaImpersonation(this IServiceCollection container, System.Action<ImpersonationOptions>? configure = null)
    {
        var options = new ImpersonationOptions();
        configure?.Invoke(options);

        container.AddSingleton(options);
        container.AddSingleton<ImpersonationCookie>();
        container.TryAddScoped<IImpersonationContext, ImpersonationContext>();
        // The routes ship with the feature rather than waiting for the host to scan this assembly:
        // a middleware nothing can turn on is not a feature. TryAddEnumerable keeps it idempotent for a
        // host that also runs AddSencillaEndpoints over Security.
        container.TryAddEnumerable(ServiceDescriptor.Transient<Sencilla.Web.MinimalApi.IEndpoint, ImpersonationEndpoint>());

        return container;
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
            // Resource → entity type mapping for startup constraint validation, one
            // DI singleton per secured entity (not process-static — no cross-host
            // leakage). The validator (registered once) reads them all back.
            container.AddSingleton(new SecurityResourceRegistration(SecurityProvider.ResourceName(type), type));
            container.TryAddEnumerable(ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, SecurityStartupValidator>());

            var constraint = typeof(SecurityConstraintHandler<>).MakeGenericType(type);

            // read interafce
            var readInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityReadingEvent<>).MakeGenericType(type));
            container.AddTransient(readInterface, constraint);

            // create: applicability gate on the -ing event, constraint enforcement on
            // the -ed post-image (inside the transaction, after role provisioning)
            var createInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatingEvent<>).MakeGenericType(type));
            container.AddTransient(createInterface, constraint);

            var createdInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatedEvent<>).MakeGenericType(type));
            container.AddTransient(createdInterface, constraint);

            // update: pre-image constraint on the -ing event, post-image re-check on -ed
            var updatingInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityUpdatingEvent<>).MakeGenericType(type));
            container.AddTransient(updatingInterface, constraint);

            var updatedInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityUpdatedEvent<>).MakeGenericType(type));
            container.AddTransient(updatedInterface, constraint);

            // delete (also fired by soft-delete Remove/Undo — both map to the Delete action)
            var deletingInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityDeletingEvent<>).MakeGenericType(type));
            container.AddTransient(deletingInterface, constraint);
        }

        // Do nothing here
        return container;
    }


}

namespace Microsoft.Extensions.DependencyInjection
{

    [DisableInjection]
    public class ServiceCollectionRegistrator : IContainer
    {
        IServiceCollection C;

        public ServiceCollectionRegistrator(IServiceCollection container) { C = container; }

        public IContainer RegisterInstance(Type srv, object instance) { C.AddSingleton(srv, instance); return this; }
        public IContainer RegisterInstance<T>(T srv) where T : class { C.AddSingleton<T>(srv); return this; }

        public IContainer RegisterType(Type srv, string? name) { if (name is not null) C.AddKeyedTransient(srv, name); else C.AddTransient(srv); return this; }
        public IContainer RegisterTypeSingleton(Type srv, string? name = null) { if (name is not null) C.AddKeyedSingleton(srv, serviceKey: name); else C.AddSingleton(srv); return this; }
        public IContainer RegisterTypePerRequest(Type srv, string? name = null) { if (name is not null) C.AddKeyedScoped(srv, name); else C.AddScoped(srv); return this; }

        public IContainer RegisterType(Type srv, Type impl, string? name) { if (name is not null) C.AddKeyedTransient(srv, name, impl); else C.AddTransient(srv, impl); return this; }
        public IContainer RegisterTypeSingleton(Type srv, Type impl, string? name = null) { if (name is not null) C.AddKeyedSingleton(srv, name, impl); else C.AddSingleton(srv, impl); return this; }
        public IContainer RegisterTypePerRequest(Type srv, Type impl, string? name = null) { if (name is not null) C.AddKeyedScoped(srv, name, impl); else C.AddScoped(srv, impl); return this; }

        public IContainer RegisterType<TSrv>(string? name = null) where TSrv : class { if (name is not null) C.AddKeyedTransient<TSrv>(name); else C.AddTransient<TSrv>(); return this; }
        public IContainer RegisterTypeSingleton<TSrv>(string? name = null) where TSrv : class { if (name is not null) C.AddKeyedSingleton<TSrv>(name); else C.AddSingleton<TSrv>(); return this; }
        public IContainer RegisterTypePerRequest<TSrv>(string? name = null) where TSrv : class { if (name is not null) C.AddKeyedScoped<TSrv>(name); else C.AddScoped<TSrv>(); return this; }

        public IContainer RegisterType<TSrv, TImpl>(string? name = null) where TSrv : class where TImpl : class, TSrv { if (name is not null) C.AddKeyedTransient<TSrv, TImpl>(name); else C.AddTransient<TSrv, TImpl>(); return this; }
        public IContainer RegisterTypeSingleton<TSrv, TImpl>(string? name = null) where TSrv : class where TImpl : class, TSrv { if (name is not null) C.AddKeyedSingleton<TSrv, TImpl>(name); else C.AddSingleton<TSrv, TImpl>(); return this; }
        public IContainer RegisterTypePerRequest<TSrv, TImpl>(string? name = null) where TSrv : class where TImpl : class, TSrv { if (name is not null) C.AddKeyedScoped<TSrv, TImpl>(name); else C.AddScoped<TSrv, TImpl>(); return this; }

    }
}

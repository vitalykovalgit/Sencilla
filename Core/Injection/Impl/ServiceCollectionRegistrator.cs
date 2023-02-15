namespace Microsoft.Extensions.DependencyInjection
{

    [DisableInjection]
    public class ServiceCollectionRegistrator : IContainer
    {
        IServiceCollection C;

        public ServiceCollectionRegistrator(IServiceCollection container) { C = container; }

        public IContainer RegisterInstance(Type srv, object instance) { C.AddSingleton(srv, instance); return this; } 
        public IContainer RegisterInstance<T>(T srv) where T : class { C.AddSingleton<T>(srv); return this; }

        public IContainer RegisterType(Type srv, string? name) {C.AddTransient(srv); return this; }
        public IContainer RegisterTypeSingleton(Type srv, string? name = null) { C.AddSingleton(srv); return this; }
        public IContainer RegisterTypePerRequest(Type srv, string? name = null) { C.AddScoped(srv); return this; }

        public IContainer RegisterType(Type srv, Type impl, string? name) { C.AddTransient(srv, impl); return this; }
        public IContainer RegisterTypeSingleton(Type srv, Type impl, string? name = null) { C.AddSingleton(srv, impl); return this; }
        public IContainer RegisterTypePerRequest(Type srv, Type impl, string? name = null) { C.AddScoped(srv, impl); return this; }

        public IContainer RegisterType<TSrv>(string? name = null) where TSrv : class { C.AddTransient<TSrv>(); return this; }
        public IContainer RegisterTypeSingleton<TSrv>(string? name = null) where TSrv : class { C.AddSingleton<TSrv>(); return this; }
        public IContainer RegisterTypePerRequest<TSrv>(string? name = null) where TSrv : class { C.AddScoped<TSrv>(); return this; }

        public IContainer RegisterType<TSrv, TImpl>(string? name = null) where TSrv : class where TImpl : class, TSrv { C.AddTransient<TSrv, TImpl>(); return this; }
        public IContainer RegisterTypeSingleton<TSrv, TImpl>(string? name = null) where TSrv : class where TImpl : class, TSrv { C.AddSingleton<TSrv, TImpl>(); return this; }
        public IContainer RegisterTypePerRequest<TSrv, TImpl>(string? name = null) where TSrv : class where TImpl : class, TSrv { C.AddScoped<TSrv, TImpl>(); return this; }

    }
}

namespace Sencilla.Authentication;

/// <summary>
/// Selects the <see cref="IUserStore"/> implementation the authentication family reads/writes users
/// through (<c>UseUsers</c>). The default <see cref="EntityFrameworkStore"/> bridges to the
/// <c>Sencilla.Component.Users</c> entities; <see cref="Store{T}"/> plugs in a custom store.
/// </summary>
public sealed class UserStoreConfig
{
    internal Action<IServiceCollection>? Registration { get; private set; }

    /// <summary>Use the EF bridge over <c>Sencilla.Component.Users</c> (default store).</summary>
    public UserStoreConfig EntityFrameworkStore()
    {
        Registration = static services => services.AddTransient<IUserStore, EfUserStore>();
        return this;
    }

    /// <summary>Plug in a custom <see cref="IUserStore"/> implementation.</summary>
    public UserStoreConfig Store<T>() where T : class, IUserStore
    {
        Registration = static services => services.AddTransient<IUserStore, T>();
        return this;
    }
}

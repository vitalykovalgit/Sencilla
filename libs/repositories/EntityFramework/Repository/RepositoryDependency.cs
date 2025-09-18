
namespace Sencilla.Repository.EntityFramework;

[PerRequestLifetime]
public class RepositoryDependency(IServiceProvider resolver, IEventDispatcher events, ICommandDispatcher commands)
{
    public IServiceProvider Resolver { get; } = resolver;
    public IEventDispatcher Events { get; } = events;
    public ICommandDispatcher Commands { get; } = commands;
}

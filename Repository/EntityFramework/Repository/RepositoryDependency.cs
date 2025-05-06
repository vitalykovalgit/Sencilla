
namespace Sencilla.Repository.EntityFramework;

[PerRequestLifetime]
public class RepositoryDependency(IResolver resolver, IEventDispatcher events, ICommandDispatcher commands)
{
    public IResolver Resolver { get; } = resolver;
    public IEventDispatcher Events { get; } = events;
    public ICommandDispatcher Commands { get; } = commands;
}

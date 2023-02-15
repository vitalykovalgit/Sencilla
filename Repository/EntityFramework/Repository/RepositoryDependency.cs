
namespace Sencilla.Repository.EntityFramework;

[PerRequestLifetime]
public class RepositoryDependency
{
    public RepositoryDependency(IResolver resolver, IEventDispatcher events, ICommandDispatcher commands)
    {
        Resolver = resolver;
        Commands = commands;
        Events = events;
    }

    public IResolver Resolver { get; }
    public IEventDispatcher Events { get; }
    public ICommandDispatcher Commands { get; }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sencilla.Repository.EntityFramework;

namespace Sencilla.Messaging.EntityFramework.Tests;

/// <summary>
/// The transport lives in an [AutoDiscovery] assembly, so the AddSencilla() scan runs over it too: its own
/// registrations must survive the scan, and the worker context QueueMessage pins must be registered from the
/// entity — whichever of AddSencilla() (the web host runs it first) and AddSencillaRepositoryForEF (the worker
/// runs it first) comes first. Built with ValidateOnBuild, the way a Development host does.
/// </summary>
public class RegistrationTests
{
    private static ServiceProvider Build(bool discoveryFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (discoveryFirst) services.AddSencilla(new ConfigurationBuilder().Build());

        services.AddSencillaRepositoryForEF(o => o.UseInMemoryDatabase("registration"));
        services.AddSencillaMessaging(m => m.UseEntityFramework(ef =>
        {
            ef.WithOptions(o => o.PollIntervalSeconds = 7);
            ef.AddStreams(s => s.AddQueue("tasks"));
        }));

        if (!discoveryFirst) services.AddSencilla(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Host_Validates_AndTheTransportKeepsItsOwnRegistrations(bool discoveryFirst)
    {
        using var provider = Build(discoveryFirst);

        Assert.Equal(7, provider.GetRequiredService<EfMessagingOptions>().PollIntervalSeconds);
        Assert.NotNull(provider.GetRequiredService<EfMessagingProviderConfig>().Streams.GetConfig("tasks"));
        Assert.Same(provider.GetRequiredService<EfMessageStreamProvider>(), provider.GetRequiredService<EfMessageStreamProvider>());

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IReadRepository<QueueMessage, Guid>>());
        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", scope.ServiceProvider.GetRequiredService<MessagingDbContext>().Database.ProviderName);
        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", scope.ServiceProvider.GetRequiredService<DynamicDbContext>().Database.ProviderName);
    }
}

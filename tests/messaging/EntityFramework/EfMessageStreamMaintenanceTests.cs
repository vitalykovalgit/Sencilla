using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sencilla.Repository.EntityFramework;

namespace Sencilla.Messaging.EntityFramework.Tests;

/// <summary>
/// A read's bookkeeping — reclaim, rescue, retention — is best effort: when it fails the read still claims, because an
/// exception out of Read stops the host that consumes the stream. In production the failure was a deadlock between the
/// rescue and a handler's Ack; here it is the InMemory provider, which runs no ExecuteUpdate at all — so every piece of
/// the bookkeeping throws, and the claim (a tracked update) still works.
/// </summary>
public class EfMessageStreamMaintenanceTests
{
    [Fact]
    public async Task Read_ClaimsTheWaitingRow_WhenItsBookkeepingFails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSencillaRepositoryForEF(o => o.UseInMemoryDatabase(nameof(Read_ClaimsTheWaitingRow_WhenItsBookkeepingFails)));
        services.AddSencillaMessaging(m => m.UseEntityFramework(ef => ef.AddStreams(s => s.AddQueue("tasks"))));
        services.AddSencilla(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        var id = Guid.NewGuid();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            db.QueueMessages.Add(new QueueMessage { Id = id, Stream = "tasks", PayloadType = "test", Payload = "{}" });
            await db.SaveChangesAsync();

            await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
                db.QueueMessages.Where(m => m.Id == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Error, "bookkeeping")));
        }

        var config = provider.GetRequiredService<EfMessagingProviderConfig>().Streams.GetConfig("tasks")!;
        var stream = provider.GetRequiredService<EfMessageStreamProvider>().GetOrCreateStream(config);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        Assert.Contains(id.ToString(), await stream.Read(timeout.Token), StringComparison.OrdinalIgnoreCase);
    }
}

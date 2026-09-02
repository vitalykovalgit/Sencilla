using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Sencilla.Repository.EntityFramework;

namespace Sencilla.Messaging.EntityFramework.Tests;

/// <summary>
/// The two [Message] entities carry their contract in attributes and marker interfaces, and both
/// fail SILENTLY when one goes missing: the EF registrar skips ICreateRepository/IUpdateRepository
/// for a type without the create/update markers, so the loss surfaces only as a DI resolution error
/// on the first enqueue or claim in a running app. These assertions turn that into a build failure.
/// </summary>
public class MessageEntityTests
{
    public static TheoryData<Type> BothEntities => new() { typeof(AppMessage), typeof(QueueMessage) };

    [Theory, MemberData(nameof(BothEntities))]
    public void BothEntities_ShareTheSameColumnsFromMessageRow(Type entity)
    {
        Assert.Equal(typeof(MessageRow), entity.BaseType);

        // Declaring none of its own is what keeps the two twins from drifting apart.
        Assert.Empty(entity.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    [Theory, MemberData(nameof(BothEntities))]
    public void BothEntities_MapTheMessageTable(Type entity)
        => Assert.Equal("Message", entity.GetCustomAttribute<TableAttribute>(inherit: false)?.Name);

    [Theory, MemberData(nameof(BothEntities))]
    public void BothEntities_AreCreateableAndUpdateable(Type entity)
    {
        // No markers => RegisterCreateRepo/RegisterUpdateRepo return early and the repository is
        // never registered. MessageQueue takes ICreateRepository<AppMessage>; the claim loop takes
        // IUpdateRepository<QueueMessage>.
        Assert.True(typeof(IEntity<Guid>).IsAssignableFrom(entity));
        Assert.True(typeof(IEntityCreateable).IsAssignableFrom(entity));
        Assert.True(typeof(IEntityUpdateable).IsAssignableFrom(entity));
    }

    /// <summary>
    /// Abstract is what keeps the shared base out of the model: entity discovery filters on
    /// !IsAbstract, so MessageRow never becomes an entity of its own and EF sees no TPH hierarchy
    /// (which would demand a discriminator column the table does not have).
    /// </summary>
    [Fact]
    public void MessageRow_IsAbstractAndNotAnEntityItself()
    {
        Assert.True(typeof(MessageRow).IsAbstract);
        Assert.Null(typeof(MessageRow).GetCustomAttribute<TableAttribute>(inherit: false));
    }

    /// <summary>Only the worker twin is pinned to the isolated context; the app twin must stay in the app's.</summary>
    [Fact]
    public void OnlyQueueMessage_IsPinnedToTheMessagingDbContext()
    {
        Assert.NotNull(typeof(QueueMessage).GetCustomAttribute(typeof(DbContextAttribute<MessagingDbContext>)));
        Assert.Null(typeof(AppMessage).GetCustomAttribute(typeof(DbContextAttribute<>)));
    }
}

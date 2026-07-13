using Moq;
using Sencilla.Core;

namespace Sencilla.Component.Users.Tests;

public class TrackStampHandlerTests
{
    private class Order : IEntity<int>, IEntityCreatedByTrack, IEntityUpdatedByTrack
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }

    private readonly TrackStampHandler<Order> _handler = new();

    private static ISystemVariable SysVarsFor(User? user) =>
        Mock.Of<ISystemVariable>(s => s.Get<User>("user") == user);

    [Fact]
    public async Task Create_StampsCurrentUser_OverwritingForgedValues()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@example.com" };
        var order = new Order { CreatedBy = Guid.NewGuid(), UpdatedBy = Guid.NewGuid() }; // forged
        var e = new EntityCreatingEvent<Order> { Entities = new[] { order }.AsQueryable() };

        await _handler.HandleAsync(e, SysVarsFor(user), CancellationToken.None);

        Assert.Equal(user.Id, order.CreatedBy);
        Assert.Equal(user.Id, order.UpdatedBy);
    }

    [Fact]
    public async Task Update_StampsUpdatedByOnly()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@example.com" };
        var original = Guid.NewGuid();
        var order = new Order { CreatedBy = original };
        var e = new EntityUpdatingEvent<Order> { Entities = new[] { order }.AsQueryable() };

        await _handler.HandleAsync(e, SysVarsFor(user), CancellationToken.None);

        Assert.Equal(user.Id, order.UpdatedBy);
        Assert.Equal(original, order.CreatedBy);
    }

    [Fact]
    public async Task AnonymousWrite_StampsNull()
    {
        var order = new Order { CreatedBy = Guid.NewGuid() };
        var e = new EntityCreatingEvent<Order> { Entities = new[] { order }.AsQueryable() };

        await _handler.HandleAsync(e, SysVarsFor(null), CancellationToken.None);

        Assert.Null(order.CreatedBy);
        Assert.Null(order.UpdatedBy);
    }
}

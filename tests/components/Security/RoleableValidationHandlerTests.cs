using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class RoleableValidationHandlerTests
{
    public class ProjectUser : IEntity<Guid>, IEntityRoleable<Guid>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid EntityId { get; set; }
        public int RoleId { get; set; }
    }

    private readonly RoleableValidationHandler<ProjectUser> _handler = new();

    private static EntityCreatingEvent<ProjectUser> Creating(params RoleType[] roles) => new()
    {
        Entities = roles.Select(r => new ProjectUser { RoleId = (int)r }).AsQueryable(),
    };

    [Theory]
    [InlineData(RoleType.Root)]
    [InlineData(RoleType.Anonymous)]
    [InlineData(RoleType.User)]
    public async Task ClaimsDerivedRoles_AreGlobalOnly_RejectedOnRoleTables(RoleType role)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(Creating(role), CancellationToken.None));
    }

    [Theory]
    [InlineData(RoleType.Owner)]
    [InlineData(RoleType.Editor)]
    [InlineData(RoleType.Viewer)]
    [InlineData(RoleType.Admin)] // admin of THIS row — a coherent, intentional capability
    public async Task HoldableRoles_Pass(RoleType role)
    {
        await _handler.HandleAsync(Creating(role), CancellationToken.None);
    }

    [Fact]
    public async Task Update_RejectsClaimsDerivedRoleToo()
    {
        var e = new EntityUpdatingEvent<ProjectUser>
        {
            Entities = new[] { new ProjectUser { RoleId = (int)RoleType.User } }.AsQueryable(),
        };

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(e, CancellationToken.None));
    }
}

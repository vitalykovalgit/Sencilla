using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class SecurityStartupValidatorTests
{
    public class Order : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int Amount { get; set; }
    }

    private static Matrix Row(string resource, string? constraint) =>
        new() { RoleId = 3, Resource = resource, Action = (int)Action.Read, Constraint = constraint };

    private static Task Validate(params Matrix[] rows)
    {
        var provider = new Mock<ISecurityProvider>();
        provider.Setup(p => p.GetAllPermissions(It.IsAny<CancellationToken>())).ReturnsAsync(rows.AsQueryable());

        var sc = new ServiceCollection();
        sc.AddSingleton(provider.Object);
        var validator = new SecurityStartupValidator(sc.BuildServiceProvider(),
            new[] { new SecurityResourceRegistration("order", typeof(Order)) });

        return validator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task UserPlaceholder_ValidMember_DoesNotBlockDeploy()
    {
        // The regression that would have crashed every Photoboost host at boot: the
        // {user} placeholder is typed (User) so `userId={user}.Id` parses fine.
        await Validate(Row("order", "userId={user}.Id"));
    }

    [Fact]
    public async Task EntityMemberTypo_NoPlaceholder_BlocksDeploy()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Validate(Row("order", "Amountt == 1")));
        Assert.Contains("Amountt", ex.Message);
    }

    [Fact]
    public async Task UserPlaceholder_MemberTypo_BlocksDeploy()
    {
        // {user} is typed, so a typo on its member is a provable error, not a maybe.
        await Assert.ThrowsAsync<InvalidOperationException>(() => Validate(Row("order", "userId={user}.Idd")));
    }

    [Fact]
    public async Task UnknownVariable_Warns_DoesNotBlockDeploy()
    {
        // A request-scoped variable we cannot type at boot -> can't be sure it's wrong -> warn, not crash.
        await Validate(Row("order", "shopId={shop}.Id"));
    }

    [Fact]
    public async Task UnknownResource_Skipped()
    {
        await Validate(Row("some-ui-view", "anything at all"));
    }

    [Fact]
    public async Task ValidPlaceholderFreeConstraint_Passes()
    {
        await Validate(Row("order", "Amount > 0"));
    }
}

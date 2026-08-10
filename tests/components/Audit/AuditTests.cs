using System.Text.Json;

namespace Sencilla.Component.Audit.Tests;

/// <summary>
/// Module 1 (audit recorder). Exercises the pure builders that the pipeline handler delegates to — Action,
/// actor attribution, field-level diff, reason, correlation — plus the actor-type resolver and the
/// registrator (an auditable type wires handlers; a non-audited type wires none → produces no rows).
/// Matches the pure-handler test style of TrackStampHandlerTests.
/// </summary>
public class AuditTests
{
    // ── test entities ──────────────────────────────────────────────────────────

    class Widget : IEntity<Guid>, IEntityAuditable
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int Qty { get; set; }

        [NotMapped] public string? Transient { get; set; }
        public Widget? Parent { get; set; }   // navigation — must be excluded
    }

    class Tracked : IEntity<Guid>, IEntityAuditable
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    class Plain : IEntity<Guid>   // NOT IEntityAuditable
    {
        public Guid Id { get; set; }
    }

    sealed class Ctx : IAuditContext
    {
        public ActorType ActorType { get; init; } = ActorType.Admin;
        public Guid? ActorId { get; init; } = Guid.NewGuid();
        public Guid? ImpersonatedById { get; init; }
        public string? Reason { get; set; } = "correction";
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
    }

    // ── impersonation attribution ─────────────────────────────────────────────

    [Fact]
    public void Rows_Carry_The_Real_Operator_When_The_Actor_Is_Impersonated()
    {
        // The row must read as the impersonated user (ActorId) while still naming who actually acted —
        // that column is the only trace, since stamps and ActorId deliberately show the customer.
        var operatorId = Guid.NewGuid();
        var ctx = new Ctx { ImpersonatedById = operatorId };
        var w = new Widget { Id = Guid.NewGuid(), Name = "A" };

        Assert.Equal(operatorId, AuditFactory.Insert(w, ctx).ImpersonatedById);
        Assert.Equal(operatorId, AuditFactory.Delete(w, ctx).ImpersonatedById);
        Assert.Equal(operatorId, AuditFactory.Update(w, new Widget { Id = w.Id, Name = "B" }, ctx)!.ImpersonatedById);
        Assert.Equal(ctx.ActorId, AuditFactory.Insert(w, ctx).ActorId);
    }

    [Fact]
    public void Rows_Leave_The_Operator_Null_On_An_Ordinary_Request()
    {
        var row = AuditFactory.Insert(new Widget { Id = Guid.NewGuid() }, new Ctx());

        Assert.Null(row.ImpersonatedById);
    }

    static Dictionary<string, JsonElement> Changes(Audit a)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(a.Changes!)!;

    // ── Insert ───────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_Records_Full_Row_With_Action_Actor_Reason_Correlation()
    {
        var ctx = new Ctx();
        var w = new Widget { Id = Guid.NewGuid(), Name = "A", Qty = 3 };

        var row = AuditFactory.Insert(w, ctx);

        Assert.Equal(AuditAction.Insert, row.Action);
        Assert.Equal("Widget", row.EntityType);
        Assert.Equal(w.Id.ToString(), row.EntityId);
        Assert.Equal(ctx.ActorType, row.ActorType);
        Assert.Equal(ctx.ActorId, row.ActorId);
        Assert.Equal(ctx.Reason, row.Reason);
        Assert.Equal(ctx.CorrelationId, row.CorrelationId);

        var changes = Changes(row);
        Assert.Equal("A", changes["name"].GetProperty("new").GetString());   // full row on insert (old = null)
        Assert.Equal(3, changes["qty"].GetProperty("new").GetInt32());
    }

    [Fact]
    public void Snapshot_Excludes_NotMapped_And_Navigation_Columns()
    {
        var row = AuditFactory.Insert(new Widget { Id = Guid.NewGuid(), Transient = "x" }, new Ctx());
        var changes = Changes(row);

        Assert.False(changes.ContainsKey("transient"));  // [NotMapped]
        Assert.False(changes.ContainsKey("parent"));      // navigation
        Assert.True(changes.ContainsKey("name"));         // scalar column present
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_Records_Only_Changed_Columns_As_Old_New()
    {
        var id = Guid.NewGuid();
        var old = new Widget { Id = id, Name = "A", Qty = 3 };
        var updated = new Widget { Id = id, Name = "B", Qty = 3 };   // only Name changed

        var row = AuditFactory.Update(old, updated, new Ctx())!;

        Assert.Equal(AuditAction.Update, row.Action);
        var changes = Changes(row);
        Assert.True(changes.ContainsKey("name"));
        Assert.False(changes.ContainsKey("qty"));           // unchanged → not recorded
        Assert.Equal("A", changes["name"].GetProperty("old").GetString());
        Assert.Equal("B", changes["name"].GetProperty("new").GetString());
    }

    [Fact]
    public void Update_With_No_Changes_Produces_No_Row()
    {
        var id = Guid.NewGuid();
        var a = new Widget { Id = id, Name = "A", Qty = 3 };
        var b = new Widget { Id = id, Name = "A", Qty = 3 };

        Assert.Null(AuditFactory.Update(a, b, new Ctx()));
    }

    [Fact]
    public void Update_Touching_Only_Timestamps_Produces_No_Row()
    {
        var id = Guid.NewGuid();
        var old = new Tracked { Id = id, Status = 1, UpdatedDate = new DateTime(2026, 1, 1) };
        var updated = new Tracked { Id = id, Status = 1, UpdatedDate = new DateTime(2026, 2, 2) };  // only UpdatedDate

        Assert.Null(AuditFactory.Update(old, updated, new Ctx()));
    }

    [Fact]
    public void Update_Records_Status_But_Not_The_Timestamp_Bump()
    {
        var id = Guid.NewGuid();
        var old = new Tracked { Id = id, Status = 1, UpdatedDate = new DateTime(2026, 1, 1) };
        var updated = new Tracked { Id = id, Status = 2, UpdatedDate = new DateTime(2026, 2, 2) };

        var changes = Changes(AuditFactory.Update(old, updated, new Ctx())!);
        Assert.True(changes.ContainsKey("status"));
        Assert.False(changes.ContainsKey("updatedDate"));   // bookkeeping stamp excluded from the diff
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_Records_Full_Row_On_The_Old_Side()
    {
        var row = AuditFactory.Delete(new Widget { Id = Guid.NewGuid(), Name = "A", Qty = 3 }, new Ctx());

        Assert.Equal(AuditAction.Delete, row.Action);
        var changes = Changes(row);
        Assert.Equal("A", changes["name"].GetProperty("old").GetString());
        Assert.Equal(JsonValueKind.Null, changes["name"].GetProperty("new").ValueKind);
    }

    // ── Actor resolution ───────────────────────────────────────────────────────

    [Fact]
    public void Actor_Is_System_When_There_Is_No_User()
        => Assert.Equal(ActorType.System, AuditContext.ResolveActorType(null));

    [Fact]
    public void Actor_Is_User_For_A_Plain_Authenticated_User()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "c@x.com" };
        user.AddRole((int)RoleType.User);
        Assert.Equal(ActorType.User, AuditContext.ResolveActorType(user));
    }

    [Fact]
    public void Actor_Is_Admin_For_A_Staff_User()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "a@x.com" };
        user.AddRole((int)RoleType.Admin);
        Assert.Equal(ActorType.Admin, AuditContext.ResolveActorType(user));
    }

    // ── Registrator (non-audited entity produces no handlers → no rows) ─────────

    [Fact]
    public void Registrator_Wires_Three_Handlers_For_An_Auditable_Entity()
    {
        var services = new ServiceCollection();
        new AuditRegistrator().Register(services, typeof(Widget));

        Assert.Contains(services, d => d.ServiceType == typeof(IEventHandlerBase<EntityCreatedEvent<Widget>>));
        Assert.Contains(services, d => d.ServiceType == typeof(IEventHandlerBase<EntityUpdatingEvent<Widget>>));
        Assert.Contains(services, d => d.ServiceType == typeof(IEventHandlerBase<EntityDeletingEvent<Widget>>));
    }

    [Fact]
    public void Registrator_Wires_Nothing_For_A_Non_Audited_Entity()
    {
        var services = new ServiceCollection();
        new AuditRegistrator().Register(services, typeof(Plain));

        Assert.Empty(services);
    }

    // ── X-Audit-Reason header ──────────────────────────────────────────────────

    [Theory]
    [InlineData("%D0%9F%D0%BE%D0%BC%D0%B8%D0%BB%D0%BA%D0%B0%20%D0%BE%D0%BF%D0%B5%D1%80%D0%B0%D1%82%D0%BE%D1%80%D0%B0", "Помилка оператора")]   // percent-encoded Cyrillic
    [InlineData("plain ascii reason", "plain ascii reason")]                                                                                    // legacy raw value
    [InlineData("100% wrong", "100% wrong")]                                                                                                    // malformed escape is left as-is
    public async Task Reason_Header_Is_Percent_Decoded(string header, string expected)
    {
        var ctx = new Ctx { Reason = null };
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton<IAuditContext>(ctx).BuildServiceProvider() };
        http.Request.Headers["X-Audit-Reason"] = header;

        await new AuditReasonMiddleware(_ => Task.CompletedTask).Invoke(http);

        Assert.Equal(expected, ctx.Reason);
    }
}

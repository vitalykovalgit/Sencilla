namespace Sencilla.Component.Audit;

/// <summary>
/// Writes one audit row per impersonation start and stop.
///
/// This lives here, subscribing, rather than in the endpoint that knows the facts, because the
/// dependency only points one way: Audit references Security (its trusted insert needs
/// <c>Access.Root()</c>), so Security cannot reference Audit to write the row itself.
///
/// The rows are the point of the feature being auditable at all. Per-write attribution only exists when
/// the operator changed something, so a purely read-only session — the common case, and the one worth
/// worrying about — would otherwise leave no trace that a customer's account had been opened.
///
/// Actor is the OPERATOR (they acted as themselves in deciding to), subject is the account opened.
/// <c>ImpersonatedById</c> stays null: these rows are not themselves impersonated actions, and "who
/// opened this customer's account" has to be findable by EntityId.
/// </summary>
public class ImpersonationAuditHandler
    : IEventHandlerBase<ImpersonationStartedEvent>
    , IEventHandlerBase<ImpersonationStoppedEvent>
{
    /// <summary>The audited "entity" these rows describe. Not a table — the subject is the session itself.</summary>
    const string EntityType = "Impersonation";

    public Task HandleAsync(ImpersonationStartedEvent @event, IAuditContext ctx, ICreateRepository<Audit, long> repo, CancellationToken token)
        => WriteAsync(@event, AuditAction.Insert, ctx, repo, token);

    public Task HandleAsync(ImpersonationStoppedEvent @event, IAuditContext ctx, ICreateRepository<Audit, long> repo, CancellationToken token)
        => WriteAsync(@event, AuditAction.Delete, ctx, repo, token);

    static async Task WriteAsync(
        ImpersonationEvent @event, AuditAction action, IAuditContext ctx, ICreateRepository<Audit, long> repo, CancellationToken token)
    {
        var started = action == AuditAction.Insert;
        var value = new { @operator = @event.Operator.Email, user = @event.Target.Email };

        var changes = new Dictionary<string, object?>
        {
            // Same {field:{old,new}} shape the rest of the log uses, so the existing audit view renders it.
            ["session"] = started ? new { old = (object?)null, @new = (object?)value } : new { old = (object?)value, @new = (object?)null },
        };

        var row = new Audit
        {
            EntityType = EntityType,
            EntityId = @event.Target.Id.ToString(),
            Action = action,
            ActorType = ActorType.Admin,
            ActorId = @event.Operator.Id,
            Reason = ctx.Reason,
            CorrelationId = ctx.CorrelationId,
            Changes = JsonSerializer.Serialize(changes, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        };

        // audit.Audit carries no per-user matrix grants — nobody writes the log directly — so this write
        // needs the same root access AuditHandler uses.
        using var rootAccess = Access.Root();
        await repo.Create(row, token);
    }
}

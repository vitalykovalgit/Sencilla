namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// The columns of [Message], shared by the two entities that map the table: <see cref="AppMessage"/>
/// in the app's own DbContext and <see cref="QueueMessage"/> in the worker's isolated one. The split
/// exists only so the claim loop runs outside the security pipeline — see <see cref="AppMessage"/>
/// for why. The columns themselves are the same table, so they live here once.
///
/// Abstract on purpose: Sencilla's entity discovery skips abstract types (see
/// RegisterEFRepositoriesForType, <c>!type.IsAbstract</c>), so this never becomes an entity, a
/// repository, or a table of its own. EF maps the inherited properties straight onto each derived
/// type — no TPH hierarchy, no discriminator column — because an unmapped base type is invisible to
/// the model.
///
/// Both derived types are created and updated by code (the queue writer and the claim loop
/// respectively), hence the create/update markers here: without them the EF registrar silently
/// skips <c>ICreateRepository</c>/<c>IUpdateRepository</c> for the type. On the app side the HTTP
/// write routes are held shut by the matrix grant (resource 'appmessage', READ only), not by these
/// markers — <c>CrudApiController</c> maps every verb regardless of them.
/// </summary>
public abstract class MessageRow : IEntity<Guid>, IEntityCreateable, IEntityUpdateable
{
    public Guid Id { get; set; }

    /// <summary>Logical queue this message belongs to; consumers claim by it.</summary>
    public string Stream { get; set; } = "";

    /// <summary>Resolution key for the payload type — see PayloadTypeRegistry.</summary>
    public string PayloadType { get; set; } = "";

    /// <summary>Payload .NET FullName; diagnostics only.</summary>
    public string? Namespace { get; set; }

    /// <summary>Short payload type name; diagnostics only.</summary>
    public string? Name { get; set; }

    public Guid? CorrelationId { get; set; }

    /// <summary>Values are <see cref="MessageState"/>.</summary>
    public byte State { get; set; }

    /// <summary>Serialized payload (camelCase, see MessageJson).</summary>
    public string? Payload { get; set; }

    /// <summary>Serialized envelope metadata dictionary.</summary>
    public string? Metadata { get; set; }

    /// <summary>The subject entity of this message, if any. No FK — a message may outlive its subject.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Initiator, when there is one.</summary>
    public Guid? UserId { get; set; }

    public int Attempts { get; set; }

    /// <summary>Claimable from this moment; null = immediately. Drives retry backoff and delays.</summary>
    public DateTime? AvailableAt { get; set; }

    /// <summary>Error CODE + detail from the last failed attempt.</summary>
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Instance holding the claim; null when unclaimed.</summary>
    public string? ProcessService { get; set; }
    public DateTime? ProcessStartDate { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

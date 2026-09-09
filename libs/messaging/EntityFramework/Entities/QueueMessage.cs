namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// Worker-side twin of <see cref="AppMessage"/> over the same [Message] table: its own DbContext
/// bypasses the web security pipeline, which a claim loop running without a request identity must
/// do — a row-scoped read would filter every claimable row away. Columns live on
/// <see cref="MessageRow"/>.
///
/// The claim acquires via <c>repository.Update(item)</c> on an AsNoTracking entity, which
/// force-marks every mapped property Modified — safe here because the table has no computed or
/// identity columns (RowVersion is a [Timestamp] concurrency token, excluded from the SET clause).
/// </summary>
[Table("Message")]
[DbContext<MessagingDbContext>]
public class QueueMessage : MessageRow;

/// <summary>Isolated worker context for the queue twin.</summary>
[DisableInjection]
public class MessagingDbContext(DbContextOptions<MessagingDbContext> options) : DbContext(options)
{
    public DbSet<QueueMessage> QueueMessages => Set<QueueMessage>();
}

namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// Rebuilds the wire envelope from a stored row. Payload and Metadata are already JSON in the
/// column, so they are spliced in as nodes rather than re-serialized. Property names are camelCase
/// to match <see cref="MessageJson"/> — the consumer reads case-insensitively, but writing the
/// platform's casing keeps stored rows greppable and consistent with every other payload.
/// </summary>
public static class MessageEnvelope
{
    public static string From(QueueMessage row)
    {
        var envelope = new JsonObject
        {
            ["id"] = row.Id,
            ["correlationId"] = row.CorrelationId ?? Guid.Empty,
            ["state"] = (int)MessageState.InProgress,
            ["name"] = row.Name,
            ["createdAt"] = row.CreatedAt,
            ["processedAt"] = row.ProcessedAt,
            ["namespace"] = row.Namespace,
            ["payloadType"] = row.PayloadType,
            ["attempts"] = row.Attempts,
            ["error"] = row.Error,
            ["entityId"] = row.EntityId,
            ["userId"] = row.UserId,
            ["metadata"] = row.Metadata is null ? null : JsonNode.Parse(row.Metadata),
            ["payload"] = row.Payload is null ? null : JsonNode.Parse(row.Payload),
        };

        return envelope.ToJsonString();
    }
}

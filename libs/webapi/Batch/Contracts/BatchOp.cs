namespace Sencilla.Web.Batch;

/// <summary>
/// The set of operations a batch step can request. Parsed case-insensitively from
/// the wire <c>op</c> token (dashes ignored, e.g. <c>get-all</c> == <c>getAll</c>).
/// </summary>
public enum BatchOp
{
    Create,
    Update,
    Upsert,
    Delete,
    Remove,
    Hide,
    Show,
    GetAll,
    GetById,
    GetCount,
}

/// <summary>
/// Helpers for classifying and parsing <see cref="BatchOp"/> values.
/// </summary>
public static class BatchOps
{
    /// <summary>True for read operations, which run outside the transaction and never abort the batch.</summary>
    public static bool IsRead(this BatchOp op) => op is BatchOp.GetAll or BatchOp.GetById or BatchOp.GetCount;

    /// <summary>True for write operations, which run inside the transaction and may be substitution sources.</summary>
    public static bool IsWrite(this BatchOp op) => !op.IsRead();

    /// <summary>Maps an op to the entity flag that must be enabled for it.</summary>
    public static BatchOpFlags ToFlag(this BatchOp op) => op switch
    {
        BatchOp.Create   => BatchOpFlags.Create,
        BatchOp.Update   => BatchOpFlags.Update,
        BatchOp.Upsert   => BatchOpFlags.Upsert,
        BatchOp.Delete   => BatchOpFlags.Delete,
        BatchOp.Remove   => BatchOpFlags.Remove,
        BatchOp.Hide     => BatchOpFlags.Hide,
        BatchOp.Show     => BatchOpFlags.Show,
        BatchOp.GetAll   => BatchOpFlags.GetAll,
        BatchOp.GetById  => BatchOpFlags.GetById,
        BatchOp.GetCount => BatchOpFlags.GetCount,
        _ => BatchOpFlags.None,
    };

    /// <summary>Parses a wire op token. Returns false for unknown ops.</summary>
    public static bool TryParse(string? raw, out BatchOp op)
    {
        op = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var normalized = raw.Replace("-", "").Replace("_", "");
        return Enum.TryParse(normalized, ignoreCase: true, out op);
    }
}

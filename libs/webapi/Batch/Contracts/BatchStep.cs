namespace Sencilla.Web.Batch;

/// <summary>
/// A single operation in a <see cref="BatchRequest"/>.
/// </summary>
public sealed class BatchStep
{
    /// <summary>Unique handle for this step, used by <c>$ref</c> and echoed in the response.</summary>
    public string Ref { get; set; } = "";

    /// <summary>Operation token (see <see cref="BatchOp"/>), e.g. <c>create</c>, <c>getAll</c>.</summary>
    public string Op { get; set; } = "";

    /// <summary>The <c>[SencillaEntity]</c> name this step targets.</summary>
    public string Entity { get; set; } = "";

    /// <summary>Entity payload for write ops. May contain top-level <c>$ref:step.field</c> values.</summary>
    public JsonElement? Body { get; set; }

    /// <summary>Filter for <c>getAll</c> / <c>getCount</c> (shape of <see cref="IFilter"/>).</summary>
    public JsonElement? Filter { get; set; }

    /// <summary>Key for <c>getById</c> and id-based <c>delete</c>.</summary>
    public JsonElement? Id { get; set; }
}

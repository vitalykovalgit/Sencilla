namespace Sencilla.Web.Batch;

/// <summary>
/// Pre-flight validation. Runs before any DB work; any error rejects the whole
/// batch (HTTP 400). The "$ref must point to an earlier write step" rule makes
/// dependency cycles structurally impossible.
/// </summary>
internal static class BatchValidator
{
    public static List<string> Validate(BatchRequest request, IBatchEntityRegistry registry, BatchOptions options)
    {
        var errors = new List<string>();

        if (!string.Equals(request.Version, "v1", StringComparison.OrdinalIgnoreCase))
            errors.Add($"Unsupported version '{request.Version}'. Expected 'v1'.");

        if (request.Steps.Count == 0)
            errors.Add("Batch contains no steps.");

        if (request.Steps.Count > options.MaxSteps)
            errors.Add($"Batch has {request.Steps.Count} steps; the maximum is {options.MaxSteps}.");

        var seenRefs = new HashSet<string>(StringComparer.Ordinal);
        var earlierWriteRefs = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < request.Steps.Count; i++)
        {
            var step = request.Steps[i];
            var where = string.IsNullOrWhiteSpace(step.Ref) ? $"step[{i}]" : $"step '{step.Ref}'";

            if (string.IsNullOrWhiteSpace(step.Ref))
                errors.Add($"{where}: missing 'ref'.");
            else if (!seenRefs.Add(step.Ref))
                errors.Add($"{where}: duplicate ref.");

            if (!BatchOps.TryParse(step.Op, out var op))
            {
                errors.Add($"{where}: unknown op '{step.Op}'.");
                continue;
            }

            if (!registry.TryGet(step.Entity, out var descriptor))
            {
                errors.Add($"{where}: unknown entity '{step.Entity}' (missing [SencillaEntity] or batch not enabled).");
                continue;
            }

            if (!descriptor.Allows(op))
                errors.Add($"{where}: entity '{step.Entity}' does not allow op '{op}'.");

            if (op.IsWrite())
            {
                ValidateWriteShape(step, op, where, earlierWriteRefs, errors);
                if (!string.IsNullOrWhiteSpace(step.Ref))
                    earlierWriteRefs.Add(step.Ref);
            }
            else if (op == BatchOp.GetById && step.Id is null)
            {
                errors.Add($"{where}: getById requires 'id'.");
            }
        }

        return errors;
    }

    private static void ValidateWriteShape(BatchStep step, BatchOp op, string where, HashSet<string> earlierWriteRefs, List<string> errors)
    {
        if (op == BatchOp.Delete)
        {
            if (step.Id is null && step.Body is null)
                errors.Add($"{where}: delete requires 'id' or 'body'.");
        }
        else if (step.Body is null)
        {
            errors.Add($"{where}: op '{op}' requires 'body'.");
        }

        if (step.Body is not { ValueKind: JsonValueKind.Object } body)
            return;

        foreach (var property in body.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
                continue;

            var raw = property.Value.GetString();
            if (raw is null || !raw.StartsWith("$ref:", StringComparison.Ordinal))
                continue;

            var target = ParseRefTarget(raw);
            if (target is null)
                errors.Add($"{where}: malformed $ref '{raw}' (expected $ref:step.field).");
            else if (!earlierWriteRefs.Contains(target))
                errors.Add($"{where}: $ref '{raw}' must point to an earlier write step.");
        }
    }

    private static string? ParseRefTarget(string raw)
    {
        var rest = raw["$ref:".Length..];
        var dot = rest.IndexOf('.');
        if (dot <= 0 || dot == rest.Length - 1)
            return null;
        return rest[..dot];
    }
}

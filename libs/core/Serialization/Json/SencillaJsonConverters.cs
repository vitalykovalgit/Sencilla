
namespace Sencilla.Core;

/// <summary>
/// Registers Sencilla's framework-wide System.Text.Json converters onto a
/// <see cref="JsonSerializerOptions"/>. Call once per JSON pipeline so behaviour is identical
/// across them: MVC (<c>Mvc.JsonOptions.JsonSerializerOptions</c>) and minimal API
/// (<c>Http.Json.JsonOptions.SerializerOptions</c>).
///
/// Contract: an empty/missing <see cref="Guid"/> key is treated as "generate one" — this requires
/// every <see cref="IEntity{TKey}"/> Guid key to be store- or EF-generated.
/// </summary>
public static class SencillaJsonConverters
{
    /// <summary>
    /// Adds the Sencilla converter set to <paramref name="options"/>. Idempotent — safe to call
    /// from multiple pipelines (or more than once) without duplicating converters.
    /// </summary>
    public static JsonSerializerOptions AddSencillaJsonConverters(this JsonSerializerOptions options)
    {
        if (!options.Converters.Any(c => c is EmptyOrNullGuidConverter))
            options.Converters.Add(new EmptyOrNullGuidConverter());

        if (!options.Converters.Any(c => c is NullableEmptyGuidConverter))
            options.Converters.Add(new NullableEmptyGuidConverter());

        return options;
    }
}

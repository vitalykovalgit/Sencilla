namespace Sencilla.Web.Batch;

/// <summary>
/// Declares an entity to the Sencilla platform under a stable wire <see cref="Name"/>
/// and opts it into batch operations via <see cref="Batch"/>.
/// <para>
/// In v1 this coexists with <c>[CrudApi]</c>; it is intended to grow into the single
/// registration point for an entity (name + API + caching + batch).
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SencillaEntityAttribute : Attribute
{
    /// <param name="name">Stable name used by batch steps and the TypeScript client.</param>
    public SencillaEntityAttribute(string name)
    {
        Name = name;
    }

    /// <summary>Wire name referenced by <c>step.entity</c>. Not the CLR type or the route.</summary>
    public string Name { get; }

    /// <summary>Operations this entity exposes to the batch API. Defaults to none.</summary>
    public BatchOpFlags Batch { get; set; } = BatchOpFlags.None;
}

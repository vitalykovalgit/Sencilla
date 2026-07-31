namespace Sencilla.Web;

/// <summary>
/// Marks an OPEN GENERIC controller <c>Something&lt;TEntity, TKey&gt;</c> as an entity API surface: it is closed
/// over every <see cref="CrudApiAttribute"/> entity and routed under that entity's route. This is how a
/// component adds endpoints to every entity's API without <c>Sencilla.Web</c> knowing the component exists —
/// see <c>TagApiController</c> in <c>Sencilla.Component.Tags</c>.
///
/// <para>The controller's own generic constraints ARE its applicability rule: one declaring
/// <c>where TEntity : IEntityTaggable</c> cannot be closed over an untagged entity and is simply skipped for
/// it, so no route is published where the feature doesn't apply.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EntityApiAttribute : Attribute
{
}

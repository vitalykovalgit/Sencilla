
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
public interface IFilter
{
    /// <summary>
    /// How much values to take
    /// </summary>
    int? Skip { get; set; }

    /// <summary>
    /// How much values to take
    /// </summary>
	int? Take { get; set; }

    /// <summary>
    /// Column name by wich we need to order 
    /// </summary>
    string[]? OrderBy { get; set; }

    /// <summary>
    /// Order direction 
    /// </summary>
    bool? Descending { get; set; }

    /// <summary>
    /// Column which need to be aggregated 
    /// used with next aggragated method 
    /// MAX, SUM, MIN, AVARAGE
    /// </summary>
    string? Aggregate { get; set; }

    /// <summary>
    /// Retrieve entity with navigation property. Values that are not navigations are ignored rather than
    /// rejected — this is client input, and <c>Include</c> fails at query-compile time, so a typo must not be a
    /// 500. Components may claim a non-navigation name and give it their own meaning: <c>?with=tags</c> is
    /// Sencilla.Component.Tags asking for a side-table hydration pass.
    /// </summary>
    public string[]? With { get; set; }

    /// <summary>
    /// Search by any fields that is varchar
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Tags to filter by, for entities implementing <c>IEntityTaggable</c> (Sencilla.Component.Tags): ANY of
    /// them matches (<c>?tag=a&amp;tag=b</c>), which is the same OR semantic every other filter property uses.
    /// Singular deliberately — a plural <c>tags</c> would collide with the tag navigation property on linked
    /// entities. Ignored by entities that are not taggable, and by hosts without the component.
    ///
    /// <para>The property stays here, in the framework's one filter contract, precisely so that the binder and
    /// every repository can carry it without depending on the component that gives it meaning.</para>
    /// </summary>
    string[]? Tag { get; set; }

    /// <summary>
    /// Point-in-time read for append-only / valid-time entities (<see cref="IEntityAppendOnlyTrack"/>):
    /// return the rows active as of this instant (UTC). Null = no temporal filtering (full history).
    /// </summary>
    DateTime? AsOf { get; set; }

    /// <summary>
    /// Contains list of properties by which we need to make select 
    /// </summary>
    IDictionary<string, FilterProperty>? Properties { get; }

    /// <summary>
    /// Add property by which user wants to filter entity 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="values"></param>
    void AddProperty(string name, Type? type, params object[] values);

}


namespace Sencilla.Core;

/// <summary>
/// Declares the business key of an append-only entity — the set of properties that identify the same
/// logical record across its appended rows/versions. On write, the current open version for this key is
/// superseded (its <see cref="IEntityAppendOnlyTrack.ActiveTo"/> is closed) and a new version inserted.
/// For currency rates the key is (From, To): <c>[BusinessKey(nameof(From), nameof(To))]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class BusinessKeyAttribute(params string[] keys) : Attribute
{
    /// <summary>Names of the properties that together form the business key.</summary>
    public string[] Keys { get; } = keys;
}

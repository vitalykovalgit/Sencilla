
namespace Sencilla.Core;

/// <summary>
/// An append-only entity that is valid-time versioned over an active period [<see cref="ActiveFrom"/>,
/// <see cref="ActiveTo"/>). Rows are immutable in content; the only mutation ever permitted is stamping
/// <see cref="ActiveTo"/> once (null → a date) to close a row when a newer version supersedes it.
///
/// "Active as of D" is a simple range check:
/// <c>(ActiveFrom is null or ActiveFrom &lt;= D) and (ActiveTo is null or ActiveTo &gt; D)</c>.
/// The transaction-time axis is the inherited <see cref="IEntityCreateableTrack.CreatedDate"/>, giving
/// lightweight bitemporal history. The business identity across versions is declared with
/// <see cref="BusinessKeyAttribute"/> and is used to supersede the current open version on write.
/// </summary>
public interface IEntityAppendOnlyTrack : IEntityAppendOnly, IEntityCreateableTrack
{
    /// <summary>Valid-time start (UTC). Null = active since the beginning.</summary>
    DateTime? ActiveFrom { get; set; }

    /// <summary>Valid-time end, exclusive (UTC). Null = open / indefinitely active.</summary>
    DateTime? ActiveTo { get; set; }
}

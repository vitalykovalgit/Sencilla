namespace Sencilla.Repository.EntityFramework.Tests.Infrastructure;

/// <summary>
/// Append-only valid-time test entity (a currency-rate stand-in): business key (<see cref="From"/>,
/// <see cref="To"/>), a <see cref="Rate"/>, and the [<see cref="ActiveFrom"/>, <see cref="ActiveTo"/>)
/// interval. Drives the supersede / scheduling / append-only-guard tests.
/// </summary>
[BusinessKey(nameof(From), nameof(To))]
public class TestRate : IEntity<int>, IEntityAppendOnlyTrack
{
    public int Id { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    public decimal Rate { get; set; }

    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
    public DateTime CreatedDate { get; set; }
}

using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;

namespace Sencilla.Repository.EntityFramework.Tests;

public class GuidKeyRow
{
    public Guid Id { get; set; }

    /// <summary>Nullable twin — `in (...)` fails on this one too, and null is a legal criterion for it.</summary>
    public Guid? OwnerId { get; set; }
}

/// <summary>
/// Why a Guid criterion is rendered as an `==` chain and never as `in (...)`.
///
/// <para>The asymmetry is genuinely surprising and looks like something to simplify away: the SAME
/// double-quoted literal that <c>ParseComparisonOperator</c> happily promotes to a Guid is rejected
/// by <c>ParseIn</c>, which calls GenerateEqual on the raw operands with no promotion step. So
/// <c>Id == "…"</c> compiles and <c>Id in ("…")</c> throws — on a single value too. The probe
/// assertions below exist so that a future "surely `in` works for Guids" edit fails here instead of
/// in production, where it is a 500 from a query string.</para>
/// </summary>
public class FilterGuidExpressionTests
{
    private static readonly Guid A = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid B = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid C = new("33333333-3333-3333-3333-333333333333");

    private static readonly GuidKeyRow[] Rows =
    [
        new() { Id = A, OwnerId = A },
        new() { Id = B, OwnerId = null },
        new() { Id = C, OwnerId = B },
    ];

    private static Guid[] Filter(string name, Type type, params object?[] values)
    {
        var prop = new FilterProperty { Query = name, Type = type };
        prop.AddValues(values);

        var expression = FilterConstraintHandler<GuidKeyRow>.ToExpression(prop)!;

        return Rows.AsQueryable().Where(expression).Select(r => r.Id).ToArray();
    }

    // ── The parser behaviour the rendering is built around ────────────────────

    [Fact]
    public void DynamicLinq_PromotesAQuotedLiteral_ForEquality()
        => Assert.Single(Rows.AsQueryable().Where($"Id == \"{A}\""));

    [Fact]
    public void DynamicLinq_DoesNotPromoteTheSameLiteral_InsideIn()
    {
        var ex = Assert.Throws<ParseException>(() => Rows.AsQueryable().Where($"Id in (\"{A}\",\"{B}\")").ToArray());
        Assert.Contains("incompatible with operand types", ex.Message);
    }

    /// <summary>One value is not the workaround — `in` fails the same way with a single element.</summary>
    [Fact]
    public void DynamicLinq_InFails_EvenForASingleValue()
        => Assert.Throws<ParseException>(() => Rows.AsQueryable().Where($"Id in (\"{A}\")").ToArray());

    /// <summary>Nor are single quotes: in Dynamic LINQ `'…'` is a CHAR literal, not a string.</summary>
    [Fact]
    public void DynamicLinq_SingleQuotesAreCharLiterals_NotAWorkaround()
        => Assert.Throws<ParseException>(() => Rows.AsQueryable().Where($"Id in ('{A}')").ToArray());

    // ── What ToExpression renders ─────────────────────────────────────────────

    [Fact]
    public void OneGuid_Narrows()
        => Assert.Equal([A], Filter(nameof(GuidKeyRow.Id), typeof(Guid), A));

    [Fact]
    public void SeveralGuids_NarrowToTheirUnion()
        => Assert.Equal([A, C], Filter(nameof(GuidKeyRow.Id), typeof(Guid), A, C));

    [Fact]
    public void NullableGuidColumn_TakesTheSameChain()
        => Assert.Equal([C], Filter(nameof(GuidKeyRow.OwnerId), typeof(Guid?), B));

    [Fact]
    public void GuidsAreNeverRenderedWithIn()
        => Assert.DoesNotContain(" in (", ToText(nameof(GuidKeyRow.Id), typeof(Guid), A, C));

    // ── Null as a criterion ───────────────────────────────────────────────────

    [Fact]
    public void LoneNull_MeansIsNull()
        => Assert.Equal([B], Filter(nameof(GuidKeyRow.OwnerId), typeof(Guid?), [null]));

    /// <summary>
    /// A null beside real values used to be dropped, contradicting the lone-null case: `?ownerId=null,B`
    /// silently meant `?ownerId=B`.
    /// </summary>
    [Fact]
    public void NullBesideAValue_MeansIsNullOrThatValue()
        => Assert.Equal([B, C], Filter(nameof(GuidKeyRow.OwnerId), typeof(Guid?), null, B));

    /// <summary>
    /// All-null used to render `X in ()` — "Expression expected", a 500 from a query string. Only the
    /// single-null case was guarded.
    /// </summary>
    [Fact]
    public void SeveralNulls_StillMeanIsNull()
        => Assert.Equal([B], Filter(nameof(GuidKeyRow.OwnerId), typeof(Guid?), null, null));

    /// <summary>The same crash was reachable on every other type, where values do go through `in (...)`.</summary>
    [Fact]
    public void SeveralNulls_OnAnInColumn_DoNotRenderAnEmptyIn()
    {
        var prop = new FilterProperty { Query = "Count", Type = typeof(int?) };
        prop.AddValues(null, null);

        Assert.Equal("Count == null", FilterConstraintHandler<GuidKeyRow>.ToExpression(prop));
    }

    [Fact]
    public void NullBesideValues_OnAnInColumn_KeepsTheIn()
    {
        var prop = new FilterProperty { Query = "Count", Type = typeof(int?) };
        prop.AddValues(null, 1, 2);

        Assert.Equal("(Count in (1,2)) || Count == null", FilterConstraintHandler<GuidKeyRow>.ToExpression(prop));
    }

    private static string ToText(string name, Type type, params object?[] values)
    {
        var prop = new FilterProperty { Query = name, Type = type };
        prop.AddValues(values);
        return FilterConstraintHandler<GuidKeyRow>.ToExpression(prop)!;
    }
}

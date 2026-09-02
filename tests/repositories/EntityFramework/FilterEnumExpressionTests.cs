using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;

namespace Sencilla.Repository.EntityFramework.Tests;

public enum FileOrigin
{
    Unknown = 0,
    Upload = 1,
    Import = 2,
}

public class EnumRow
{
    public int Id { get; set; }
    public FileOrigin Origin { get; set; }

    /// <summary>Nullable twin — the rendering has to survive a null row, and null is a legal criterion.</summary>
    public FileOrigin? Source { get; set; }
}

/// <summary>
/// An enum criterion has to render its values NUMERICALLY: interpolating an enum yields its name, so
/// the predicate came out as <c>Origin in (Upload)</c> and Dynamic LINQ went looking for a property
/// called Upload.
///
/// <para>The cast that first came with that fix — <c>int(Origin) in (1)</c> — is deliberately not here.
/// The first three assertions are why: the numeric literal needs no cast, and the cast breaks a
/// nullable enum outright.</para>
/// </summary>
public class FilterEnumExpressionTests
{
    private static readonly EnumRow[] Rows =
    [
        new() { Id = 1, Origin = FileOrigin.Upload,  Source = FileOrigin.Upload },
        new() { Id = 2, Origin = FileOrigin.Import,  Source = null },
        new() { Id = 3, Origin = FileOrigin.Unknown, Source = FileOrigin.Import },
    ];

    private static string Text(string name, Type type, params object?[] values)
    {
        var prop = new FilterProperty { Query = name, Type = type };
        prop.AddValues(values);
        return FilterConstraintHandler<EnumRow>.ToExpression(prop)!;
    }

    private static int[] Filter(string name, Type type, params object?[] values)
        => Rows.AsQueryable().Where(Text(name, type, values)).Select(r => r.Id).ToArray();

    // ── The parser behaviour the rendering is built around ────────────────────

    /// <summary>The bug: an interpolated enum is its NAME, which Dynamic LINQ reads as an identifier.</summary>
    [Fact]
    public void DynamicLinq_RejectsAnEnumName_AsABareLiteral()
    {
        var ex = Assert.Throws<ParseException>(() => Rows.AsQueryable().Where($"Origin in ({FileOrigin.Upload})").ToArray());
        Assert.Contains("No property or field 'Upload' exists", ex.Message);
    }

    /// <summary>And the numeric literal needs no cast — on the nullable column too.</summary>
    [Fact]
    public void DynamicLinq_ComparesAnEnumToANumericLiteral_Uncast()
    {
        Assert.Single(Rows.AsQueryable().Where("Origin in (1)"));
        Assert.Single(Rows.AsQueryable().Where("Source in (1)"));
    }

    /// <summary>
    /// Why the cast is not used: it is evaluated per row, and a null row has nothing to cast. This is a
    /// runtime failure on any nullable enum column that actually holds a null.
    /// </summary>
    [Fact]
    public void DynamicLinq_CastOfANullableEnum_ThrowsOnANullRow()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Rows.AsQueryable().Where("int(Source) in (1)").ToArray());
        Assert.Contains("Nullable object must have a value", ex.Message);
    }

    // ── What ToExpression renders ─────────────────────────────────────────────

    [Fact]
    public void OneEnumValue_Narrows()
        => Assert.Equal([1], Filter(nameof(EnumRow.Origin), typeof(FileOrigin), FileOrigin.Upload));

    [Fact]
    public void SeveralEnumValues_NarrowToTheirUnion()
        => Assert.Equal([1, 2], Filter(nameof(EnumRow.Origin), typeof(FileOrigin), FileOrigin.Upload, FileOrigin.Import));

    /// <summary>Zero is a real enum member, not "unset" — it must not be confused with a null criterion.</summary>
    [Fact]
    public void ZeroValuedMember_Narrows()
        => Assert.Equal([3], Filter(nameof(EnumRow.Origin), typeof(FileOrigin), FileOrigin.Unknown));

    [Fact]
    public void EnumIsRenderedNumerically()
        => Assert.Equal("Origin in (1,2)",
            Text(nameof(EnumRow.Origin), typeof(FileOrigin), FileOrigin.Upload, FileOrigin.Import));

    // ── Nullable enum ─────────────────────────────────────────────────────────

    [Fact]
    public void NullableEnumColumn_Narrows()
        => Assert.Equal([3], Filter(nameof(EnumRow.Source), typeof(FileOrigin?), FileOrigin.Import));

    [Fact]
    public void LoneNull_MeansIsNull()
        => Assert.Equal([2], Filter(nameof(EnumRow.Source), typeof(FileOrigin?), [null]));

    /// <summary>The seam with null handling: both arms in one predicate must parse and evaluate.</summary>
    [Fact]
    public void NullBesideAnEnumValue_MeansIsNullOrThatValue()
        => Assert.Equal([1, 2], Filter(nameof(EnumRow.Source), typeof(FileOrigin?), null, FileOrigin.Upload));

    [Fact]
    public void NullBesideAnEnumValue_RendersBothArms()
        => Assert.Equal("(Source in (1)) || Source == null",
            Text(nameof(EnumRow.Source), typeof(FileOrigin?), null, FileOrigin.Upload));

    /// <summary>All-null must not render `Source in ()`, which is a parse exception.</summary>
    [Fact]
    public void SeveralNulls_StillMeanIsNull()
        => Assert.Equal([2], Filter(nameof(EnumRow.Source), typeof(FileOrigin?), null, null));
}

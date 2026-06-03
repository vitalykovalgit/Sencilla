namespace Sencilla.Core.Tests;

/// <summary>
/// Tests for <see cref="EmptyOrNullGuidConverter"/>, <see cref="NullableEmptyGuidConverter"/>
/// and <see cref="SencillaJsonConverters.AddSencillaJsonConverters"/>.
///
/// The behaviour matrix:
///   Guid  (key): ""/null  => Guid.Empty;   valid => parsed
///   Guid? (fk) : ""/null  => null;         valid => parsed
/// </summary>
public class GuidConvertersTests
{
    private static readonly Guid Sample = Guid.Parse("e7bd433e-f36b-1410-87c8-0046c9fd7320");

    private static JsonSerializerOptions Options()
        => new JsonSerializerOptions().AddSencillaJsonConverters();

    private class Pk { public Guid Id { get; set; } }
    private class Fk { public Guid? BasketId { get; set; } }

    // ── Non-nullable Guid key ────────────────────────────────────────────────

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("null")]
    public void Guid_Empty_Or_Null_Becomes_Empty(string idJson)
    {
        var pk = JsonSerializer.Deserialize<Pk>($"{{\"Id\":{idJson}}}", Options());

        Assert.NotNull(pk);
        Assert.Equal(Guid.Empty, pk!.Id);
    }

    [Fact]
    public void Guid_Valid_Is_Parsed()
    {
        var pk = JsonSerializer.Deserialize<Pk>($"{{\"Id\":\"{Sample}\"}}", Options());

        Assert.NotNull(pk);
        Assert.Equal(Sample, pk!.Id);
    }

    // ── Nullable Guid? optional FK ───────────────────────────────────────────

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("null")]
    public void NullableGuid_Empty_Or_Null_Becomes_Null(string fkJson)
    {
        var fk = JsonSerializer.Deserialize<Fk>($"{{\"BasketId\":{fkJson}}}", Options());

        Assert.NotNull(fk);
        Assert.Null(fk!.BasketId);
    }

    [Fact]
    public void NullableGuid_Valid_Is_Parsed()
    {
        var fk = JsonSerializer.Deserialize<Fk>($"{{\"BasketId\":\"{Sample}\"}}", Options());

        Assert.NotNull(fk);
        Assert.Equal(Sample, fk!.BasketId);
    }

    // ── Round-trip write ─────────────────────────────────────────────────────

    [Fact]
    public void Write_RoundTrips_Key_And_NullFk()
    {
        var json = JsonSerializer.Serialize(new Pk { Id = Sample }, Options());
        Assert.Contains(Sample.ToString(), json);

        var fkJson = JsonSerializer.Serialize(new Fk { BasketId = null }, Options());
        Assert.Contains("null", fkJson);
    }

    // ── Helper is idempotent ─────────────────────────────────────────────────

    [Fact]
    public void AddSencillaJsonConverters_Is_Idempotent()
    {
        var options = new JsonSerializerOptions()
            .AddSencillaJsonConverters()
            .AddSencillaJsonConverters();

        Assert.Single(options.Converters, c => c is EmptyOrNullGuidConverter);
        Assert.Single(options.Converters, c => c is NullableEmptyGuidConverter);
    }
}

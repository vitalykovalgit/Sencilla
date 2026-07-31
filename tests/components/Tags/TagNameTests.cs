namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// The normalisation contract every tag repository, endpoint and consumer depends on. It exists because engine-side
/// comparisons are ordinal while SQL Server's collation is case-insensitive: without normalisation the two
/// disagree, and a tag that the admin list finds is a silent miss in the engine.
/// </summary>
public class TagNameTests
{
    [Theory]
    [InlineData("Summer", "summer")]
    [InlineData("  SUMMER  ", "summer")]
    [InlineData("promo:Black-Friday", "promo:black-friday")]
    [InlineData("v1.2_beta", "v1.2_beta")]
    public void One_LowercasesAndTrims(string input, string expected)
        => Assert.Equal(expected, TagName.One(input));

    [Theory]
    [InlineData("black friday")]   // spaces are not tags — reject, never rewrite to black-friday
    [InlineData("promo!")]
    [InlineData("тег")]
    [InlineData("a,b")]
    [InlineData("\"quoted\"")]
    public void One_RejectsCharactersOutsideTheAllowedSet(string input)
        => Assert.Equal("tag-invalid", Assert.Throws<BadRequestException>(() => TagName.One(input)).Message);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void One_RejectsBlank(string? input)
        => Assert.Equal("tag-empty", Assert.Throws<BadRequestException>(() => TagName.One(input)).Message);

    [Fact]
    public void One_RejectsOverLength()
        => Assert.Equal("tag-too-long",
            Assert.Throws<BadRequestException>(() => TagName.One(new string('a', TagName.MaxLength + 1))).Message);

    [Fact]
    public void One_AcceptsExactlyMaxLength()
        => Assert.Equal(TagName.MaxLength, TagName.One(new string('a', TagName.MaxLength)).Length);

    [Fact]
    public void Set_DedupesCaseInsensitivelyAndSortsOrdinally()
        => Assert.Equal(["alpha", "beta", "gamma"], TagName.Set(["Gamma", "alpha", "BETA", "gamma", "  beta "]));

    [Fact]
    public void Set_OfNullOrEmpty_IsEmpty()
    {
        Assert.Empty(TagName.Set(null));
        Assert.Empty(TagName.Set([]));
    }

    [Fact]
    public void Set_RejectsWhenTheSerialisedSetWouldOverflowTheColumn()
    {
        // Distinct max-length tags until the JSON array cannot fit NVARCHAR(4000) — the check that turns a SQL
        // truncation 500 into a 400.
        var tags = Enumerable.Range(0, 200).Select(i => $"{i:D3}".PadRight(TagName.MaxLength, 'a')).ToArray();

        Assert.Equal("tag-set-too-large", Assert.Throws<BadRequestException>(() => TagName.Set(tags)).Message);
    }

    [Fact]
    public void TrySet_DropsMalformedInsteadOfThrowing()
        => Assert.Equal(["good"], TagName.TrySet(["Good", "bad tag", "", null, new string('x', 999)]));

    [Fact]
    public void TryOne_NormalisesOrReturnsNull()
    {
        Assert.Equal("summer", TagName.TryOne(" Summer "));
        Assert.Null(TagName.TryOne("no spaces allowed"));
        Assert.Null(TagName.TryOne(null));
    }
}

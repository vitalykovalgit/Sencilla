namespace Sencilla.Component.I18n.Tests;

public class TranslationHashTests
{
    [Fact]
    public void SameTextHashesTheSameAcrossCalls()
        => Assert.Equal(TranslationHash.Of("Кошик порожній"), TranslationHash.Of("Кошик порожній"));

    [Fact]
    public void DifferentTextHashesDifferently()
        => Assert.NotEqual(TranslationHash.Of("Кошик порожній"), TranslationHash.Of("Кошик порожнiй"));

    // Re-indenting a string in a textarea is not a translation-invalidating change. Treating it as
    // one would push the whole catalog back through a paid provider for a whitespace diff.
    [Theory]
    [InlineData("  Кошик порожній  ")]
    [InlineData("Кошик\n\nпорожній")]
    [InlineData("Кошик\tпорожній")]
    public void WhitespaceIsNormalisedBeforeHashing(string variant)
        => Assert.Equal(TranslationHash.Of("Кошик порожній"), TranslationHash.Of(variant));

    [Fact]
    public void NullAndEmptyHashAlike()
        => Assert.Equal(TranslationHash.Of(null), TranslationHash.Of(""));

    [Fact]
    public void IsStaleWhenTheSourceChanged()
        => Assert.True(TranslationHash.IsStale(TranslationHash.Of("Стара назва"), "Нова назва"));

    [Fact]
    public void IsNotStaleWhenTheSourceIsUnchanged()
        => Assert.False(TranslationHash.IsStale(TranslationHash.Of("Назва"), "Назва"));

    // A row written before provenance existed cannot be PROVEN current, and re-running one string
    // is cheaper than shipping a translation nobody can account for.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ARowWithNoRecordedHashIsStale(string? stored)
        => Assert.True(TranslationHash.IsStale(stored, "Назва"));

    [Fact]
    public void ComparisonIgnoresHexCasing()
        => Assert.False(TranslationHash.IsStale(TranslationHash.Of("Назва").ToUpperInvariant(), "Назва"));
}

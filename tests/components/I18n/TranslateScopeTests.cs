namespace Sencilla.Component.I18n.Tests;

/// <summary>
/// Which rows a run touches. This is the whole safety story of "Translate All": press it twice and
/// nothing a person wrote may change, and press it after a copy edit and only the edited strings
/// should cost anything.
/// </summary>
public class TranslateScopeTests
{
    private const string Source = "Кошик порожній";

    private static Translation Row(string value, TranslationOrigin origin = TranslationOrigin.Machine, string? sourceText = Source)
        => new() { Value = value, Origin = origin, SourceHash = sourceText == null ? null : TranslationHash.Of(sourceText) };

    private static TranslateSettings Settings(bool onlyEmpty = false, bool onlyStale = false, bool overwriteHuman = false)
        => new() { OnlyEmpty = onlyEmpty, OnlyStale = onlyStale, OverwriteHuman = overwriteHuman };

    [Fact]
    public void AMissingRowIsAlwaysInScope()
        => Assert.True(TranslateService.ShouldTranslate(null, Source, Settings()));

    [Fact]
    public void WithNoNarrowingEveryRowIsInScope()
        => Assert.True(TranslateService.ShouldTranslate(Row("Basket is empty"), Source, Settings()));

    // The guarantee that makes the feature usable: no combination of flags reaches a human edit.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AHumanEditIsNeverOverwritten(bool onlyEmpty, bool onlyStale)
    {
        var human = Row("Кошик порожній, як моя душа", TranslationOrigin.Human, sourceText: "щось інше");

        Assert.False(TranslateService.ShouldTranslate(human, Source, Settings(onlyEmpty, onlyStale)));
    }

    [Fact]
    public void AHumanEditIsOverwrittenOnlyWhenExplicitlyAsked()
    {
        var human = Row("Кошик порожній, як моя душа", TranslationOrigin.Human);

        Assert.True(TranslateService.ShouldTranslate(human, Source, Settings(overwriteHuman: true)));
    }

    [Fact]
    public void OnlyEmptySkipsARowThatAlreadyHasAValue()
        => Assert.False(TranslateService.ShouldTranslate(Row("Basket is empty"), Source, Settings(onlyEmpty: true)));

    [Fact]
    public void OnlyEmptyTakesARowWithNoValue()
        => Assert.True(TranslateService.ShouldTranslate(Row(""), Source, Settings(onlyEmpty: true)));

    [Fact]
    public void OnlyStaleTakesARowWhoseSourceChanged()
        => Assert.True(TranslateService.ShouldTranslate(Row("Basket is empty", sourceText: "Кошик порожній (стара редакція)"), Source, Settings(onlyStale: true)));

    [Fact]
    public void OnlyStaleSkipsARowStillMatchingItsSource()
        => Assert.False(TranslateService.ShouldTranslate(Row("Basket is empty"), Source, Settings(onlyStale: true)));

    // A row from before provenance has no hash, so it cannot be shown to be current.
    [Fact]
    public void OnlyStaleTakesARowWithNoRecordedSource()
        => Assert.True(TranslateService.ShouldTranslate(Row("Basket is empty", sourceText: null), Source, Settings(onlyStale: true)));

    [Fact]
    public void BothFlagsTakeEitherKind()
    {
        var empty = Row("");
        var stale = Row("Basket is empty", sourceText: "стара редакція");
        var current = Row("Basket is empty");

        Assert.True(TranslateService.ShouldTranslate(empty, Source, Settings(onlyEmpty: true, onlyStale: true)));
        Assert.True(TranslateService.ShouldTranslate(stale, Source, Settings(onlyEmpty: true, onlyStale: true)));
        Assert.False(TranslateService.ShouldTranslate(current, Source, Settings(onlyEmpty: true, onlyStale: true)));
    }
}

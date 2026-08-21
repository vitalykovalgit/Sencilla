namespace Sencilla.Component.I18n;

/// <summary>
/// One translation run, as the admin configures it.
/// </summary>
public class TranslateSettings
{
    /// <summary>Registered translator to use. Null takes the one marked default.</summary>
    public string ProviderName { get; set; } = default!;

    /// <summary>Only rows with no value yet. Combine with <see cref="OnlyStale"/> to get both.</summary>
    public bool OnlyEmpty { get; set; }

    /// <summary>
    /// Only rows whose source text has changed since they were translated (see
    /// <see cref="TranslationHash"/>). This is what makes a re-run cheap: after a copy edit to three
    /// Ukrainian strings, a stale run sends three strings to the provider, not the whole catalog.
    /// </summary>
    public bool OnlyStale { get; set; }

    /// <summary>
    /// Language the run reads FROM. Null falls back to <see cref="Resource.Description"/>, which is
    /// the source text a developer wrote. Picking a language here lets an admin translate from a
    /// reviewed English pass rather than from the original, which is what a translator would do.
    /// </summary>
    public int? SourceLanguageId { get; set; }

    /// <summary>
    /// Languages to write. The service translates one at a time; the controller loops.
    /// </summary>
    public int[]? LanguageIds { get; set; }

    /// <summary>
    /// Replace values a person edited. OFF by default and deliberately awkward to turn on: the
    /// whole point of <see cref="TranslationOrigin.Human"/> is that a scheduled run cannot quietly
    /// undo a country manager's correction.
    /// </summary>
    public bool OverwriteHuman { get; set; }
}

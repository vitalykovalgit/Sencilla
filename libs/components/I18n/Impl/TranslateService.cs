namespace Sencilla.Component.I18n;

/// <summary>
/// Runs one language through a translation provider.
///
/// Three things make a re-run safe to schedule: it never overwrites a value a person edited, it can
/// be narrowed to rows whose source text actually changed, and every row it writes records what it
/// was translated from. Without those, "Translate All" is a button nobody dares press twice.
/// </summary>
public class TranslateService : ITranslateService
{
    private const int BatchSize = 20;

    private readonly ITranslator[] _translators;
    private readonly IReadRepository<ClientLanguage> _clientLanguageReadRepo;
    private readonly IReadRepository<Language> _languageReadRepo;
    private readonly IReadRepository<Translation> _translationReadRepo;
    private readonly ICreateRepository<Translation> _translationCreateRepo;
    private readonly IReadRepository<Resource, string> _resourceReadRepo;

    public TranslateService(
        IEnumerable<ITranslator> translator,
        IReadRepository<Translation> translationReadRepo,
        IReadRepository<Resource, string> resourceReadRepo,
        ICreateRepository<Translation> translationCreateRepo,
        IReadRepository<ClientLanguage> languageReadRepo,
        IReadRepository<Language> languages)
    {
        _translators = translator.ToArray();
        _translationReadRepo = translationReadRepo;
        _resourceReadRepo = resourceReadRepo;
        _translationCreateRepo = translationCreateRepo;
        _clientLanguageReadRepo = languageReadRepo;
        _languageReadRepo = languages;
    }

    public async Task TranslateLanguage(int languageId, TranslateSettings translateSettings)
    {
        var translator = translateSettings.ProviderName == null
            ? _translators.FirstOrDefault(p => p.Default)
            : _translators.FirstOrDefault(p => p.Name == translateSettings.ProviderName)
                ?? throw new ApplicationException($"The translator with name {translateSettings.ProviderName} is not registered in the app.");

        if (translator == null)
            throw new ApplicationException("No translation provider is registered.");

        var clientLanguage = (await _clientLanguageReadRepo.GetAll(with: with => with.Language)).FirstOrDefault(p => p.LanguageId == languageId)
            ?? throw new ApplicationException($"The language with id {languageId} can't be translated since it has not added into app.");

        var source = await ResolveSourceAsync(translateSettings.SourceLanguageId);

        // Translating a language from itself would overwrite it with its own text.
        if (source.LanguageId == languageId)
            return;

        var resources = await _resourceReadRepo.GetAll();
        var translations = await _translationReadRepo.GetAll(new TranslationFilter().ByLanguageId(clientLanguage.LanguageId));
        var byResource = translations.GroupBy(t => t.ResourceId).ToDictionary(g => g.Key, g => g.First());

        // Source text per resource, resolved once: it is read again after the provider call to
        // fingerprint what was sent, and a lookup per row there would be a scan per row.
        var sourceTexts = new Dictionary<string, string>();
        var pending = new List<TranslateDefinition>();

        foreach (var resource in resources)
        {
            var text = source.TextFor(resource);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sourceTexts[resource.Id] = text;

            byResource.TryGetValue(resource.Id, out var existing);

            if (!ShouldTranslate(existing, text, translateSettings))
                continue;

            pending.Add(new TranslateDefinition
            {
                Text = text,
                Translation = existing ?? new Translation { ResourceId = resource.Id, LanguageId = clientLanguage.LanguageId }
            });
        }

        if (pending.Count == 0)
            return;

        var transformer = new NumericTranslateTextTransform();

        foreach (var batch in pending.Chunk(BatchSize))
        {
            var batchList = batch.ToList();

            batchList.ForEach(transformer.Transform);

            var translated = await translator.TranslateText(batchList.Select(p => p.Text).ToArray(), source.Locale, clientLanguage.Language!.Locale);

            for (var i = 0; i < batchList.Count; i++)
                batchList[i].Text = translated[i];

            batchList.ForEach(transformer.TransformBack);
        }

        var now = DateTime.UtcNow;

        foreach (var definition in pending)
        {
            definition.Translation.Value = definition.Text;
            definition.Translation.Origin = TranslationOrigin.Machine;
            definition.Translation.SourceLanguageId = source.LanguageId;
            // Hash the SOURCE text, not the produced translation — `Text` was overwritten with the
            // provider's answer above, so the fingerprint has to come from what we sent.
            definition.Translation.SourceHash = TranslationHash.Of(sourceTexts[definition.Translation.ResourceId]);
            definition.Translation.UpdatedDate = now;
        }

        await _translationCreateRepo.UpsertAsync(pending.Select(p => p.Translation).ToList(), x => x.Id);
    }

    /// <summary>
    /// Whether this row is in scope for the run. With neither narrowing flag set every row is in
    /// scope, which is the "retranslate everything" case an admin sometimes genuinely wants.
    /// </summary>
    internal static bool ShouldTranslate(Translation? existing, string sourceText, TranslateSettings settings)
    {
        // A person's edit outranks any run. This is checked FIRST so that no combination of the
        // other flags can reach it — the guarantee has to be unconditional to be worth anything.
        if (existing is { Origin: TranslationOrigin.Human } && !settings.OverwriteHuman)
            return false;

        var empty = existing == null || string.IsNullOrEmpty(existing.Value);
        var stale = TranslationHash.IsStale(existing?.SourceHash, sourceText);

        if (settings.OnlyEmpty && settings.OnlyStale) return empty || stale;
        if (settings.OnlyEmpty) return empty;
        if (settings.OnlyStale) return stale;

        return true;
    }

    /// <summary>
    /// Resolves what the run reads FROM: a chosen language's own translations, or the resource
    /// descriptions a developer wrote.
    /// </summary>
    private async Task<TranslationSource> ResolveSourceAsync(int? sourceLanguageId)
    {
        if (sourceLanguageId == null)
            return new TranslationSource(null, DefaultSourceLocale, null);

        var language = (await _languageReadRepo.GetAll()).FirstOrDefault(l => l.Id == sourceLanguageId)
            ?? throw new ApplicationException($"The source language with id {sourceLanguageId} does not exist.");

        var values = (await _translationReadRepo.GetAll(new TranslationFilter().ByLanguageId(language.Id)))
            .GroupBy(t => t.ResourceId)
            .ToDictionary(g => g.Key, g => g.First().Value);

        return new TranslationSource(language.Id, language.Locale, values);
    }

    /// <summary>
    /// Locale claimed for <see cref="Resource.Description"/> when no source language is picked.
    /// Descriptions are whatever language the developer wrote them in; providers need SOME locale,
    /// and this preserves the behaviour every existing run was configured against.
    /// </summary>
    private const string DefaultSourceLocale = "en";

    /// <summary>Where a run's source text comes from — a language's translations, or the resource itself.</summary>
    private sealed record TranslationSource(int? LanguageId, string Locale, Dictionary<string, string>? Values)
    {
        public string? TextFor(Resource resource) =>
            Values == null ? resource.Description : (Values.TryGetValue(resource.Id, out var value) ? value : null);
    }
}

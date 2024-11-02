namespace Sencilla.Component.I18n;

// TODO: test this service
public class TranslateService : ITranslateService
{
    private const string sourceLocale = "en";
    private const int batchSize = 20;

    private readonly ITranslator[] _translators;
    private readonly IReadRepository<ClientLanguage> _clientLanguageReadRepo;
    private readonly IReadRepository<Translation> _translationReadRepo;
    private readonly ICreateRepository<Translation> _translationCreateRepo;
    private readonly IReadRepository<Resource, string> _resourceReadRepo;

    public TranslateService(
        IEnumerable<ITranslator> translator,
        IReadRepository<Translation> translationReadRepo,
        IReadRepository<Resource, string> resourceReadRepo,
        ICreateRepository<Translation> translationCreateRepo,
        IReadRepository<ClientLanguage> languageReadRepo)
    {
        _translators = translator.ToArray();
        _translationReadRepo = translationReadRepo;
        _resourceReadRepo = resourceReadRepo;
        _translationCreateRepo = translationCreateRepo;
        _clientLanguageReadRepo = languageReadRepo;
    }

    public async Task TranslateLanguage(int languageId, TranslateSettings translateSettings)
    {
        var translator = translateSettings.ProviderName == null ? _translators.FirstOrDefault(p => p.Default) : _translators.FirstOrDefault(p => p.Name == translateSettings.ProviderName)
            ?? throw new ApplicationException($"The translator with name {translateSettings.ProviderName} is not registered in the app.");

        var clientLanguage = (await _clientLanguageReadRepo.GetAll(with: with => with.Language)).FirstOrDefault(p => p.LanguageId == languageId)
            ?? throw new ApplicationException($"The language with id {languageId} can't be translated since it has not added into app.");

        var resources = await _resourceReadRepo.GetAll();
        var translations = await _translationReadRepo.GetAll(new TranslationFilter().ByLanguageId(clientLanguage.LanguageId));

        var query = from resource in resources
                    join translation in translations on resource.Id equals translation.ResourceId into ts
                    from translation in ts.DefaultIfEmpty()
                    select new TranslateDefinition
                    {
                        Text = resource.Description,
                        Translation = translation ?? new Translation { ResourceId = resource.Id, LanguageId = clientLanguage.LanguageId }
                    };

        var translateDefinitions = query
            .Where(p => !translateSettings.OnlyEmpty || string.IsNullOrEmpty(p.Translation.Value))
            .ToList();

        var transformer = new NumericTranslateTextTransform();

        var batches = translateDefinitions.Chunk(batchSize);

        foreach (var batch in batches)
        {
            var batchList = batch.ToList();

            batchList.ForEach(transformer.Transform);

            var translation = await translator.TranslateText(batchList.Select(p => p.Text).ToArray(), sourceLocale, clientLanguage.Language.Locale);

            for (int i = 0; i < batchList.Count; i++)
                batchList[i].Text = translation[i];

            batchList.ForEach(transformer.TransformBack);
        }

        translateDefinitions.ForEach(p => p.Translation.Value = p.Text);

        var translationsToUpdate = translateDefinitions.Select(p => p.Translation).ToList();

        await _translationCreateRepo.UpsertAsync(translationsToUpdate, x => x.Id);
    }
}

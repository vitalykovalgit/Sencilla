namespace Sencilla.Component.I18n;

public class GoogleTranslator : ITranslator
{
    public string Name => nameof(GoogleTranslator);

    public bool Default => true;

    private readonly GoogleTranslatorOptions _options;
    //private GoogleCredential _credential;

    public GoogleTranslator(IOptions<GoogleTranslatorOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string[]> TranslateText(string[] values, string from, string to)
    {
        //var credential = await GetGoogleCredential();

        var client = new TranslationServiceClientBuilder().Build();

        var request = new TranslateTextRequest()
        {
            Contents = { values },
            SourceLanguageCode = from,
            TargetLanguageCode = to,
            Parent = $"projects/{_options.Project}"
        };
        var response = await client.TranslateTextAsync(request);

        return response.Translations.Select(t => t.TranslatedText).ToArray();
    }

    public async Task<string[]> GetSupportedLanguages()
    {
        //var credential = await GetGoogleCredential();

        var client = new TranslationServiceClientBuilder().Build();

        var request = new GetSupportedLanguagesRequest() { Parent = $"projects/{_options.Project}" };
        var response = await client.GetSupportedLanguagesAsync(request);

        return response.Languages.Select(l => l.LanguageCode).ToArray();
    }

    //private async Task<GoogleCredential> GetGoogleCredential()
    //{
    //    if (_credential is null)
    //    {
    //        var json = new StringBuilder(await File.ReadAllTextAsync(_options.CredentialsFile));
    //        json.Replace(GoogleTranslatorOptions.PrivateKeyParam, _options.PrivateKey);

    //        _credential = GoogleCredential.FromJson(json.ToString());
    //    }

    //    return _credential;
    //}
}

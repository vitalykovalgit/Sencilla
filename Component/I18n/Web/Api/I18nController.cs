namespace Sencilla.Component.I18n;

[Route("api/v1/i18n")]
public class I18nController : ApiController
{
    private readonly ILocalizationProvider _localizationProvider;
    private readonly ISupportedLanguagesProvider _supportedLanguagesProvider;

    public I18nController(
        IResolver resolver,
        ILocalizationProvider localizationProvider,
        ISupportedLanguagesProvider supportedLanguagesProvider) : base(resolver)
    {
        _localizationProvider = localizationProvider;
        _supportedLanguagesProvider = supportedLanguagesProvider;
    }

    [HttpPost]
    [Route("{ns}/{locale}.json")]
    [Route("{locale}.json")]
    public async Task<IActionResult> GetJson(string locale, string ns)
    {
        var translations = string.IsNullOrEmpty(ns)
            ? (await _localizationProvider.GetStrings(locale)).AsEnumerable()
            : (await _localizationProvider.GetStringsByGroup(ns, locale)).AsEnumerable();

        var translationDictionary = translations.ToDictionary(t => t.Key, t => t.Value);

        return Ok(translationDictionary);
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("langs")]
    public async Task<IActionResult> GetLangs()
    {
        var langs = (await _supportedLanguagesProvider.GetSupportedLanguages())
            .OrderBy(x => x.Order).Select(p => new LanguageWe(p)).ToList();

        return Ok(langs);
    }
}

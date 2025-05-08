namespace Sencilla.Component.I18n;

/// <summary>
/// TODO: Move to base controller 
/// </summary>
[Route("api/v1/i18n")]
public class I18nController : ApiController
{
    private readonly ILocalizationProvider _localizationProvider;

    public I18nController(IResolver resolver, ILocalizationProvider localizationProvider) : base(resolver)
    {
        _localizationProvider = localizationProvider;
    }

    [HttpGet]
    [Route("{locale}.json")]
    [Route("{ns}/{locale}.json")]
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
        //var langs = (await _supportedLanguagesProvider.GetSupportedLanguages())
        //    .OrderBy(x => x.Order).Select(p => new LanguageWe(p)).ToList();
        //return Ok(langs);
        var langs = (await R<IReadRepository<ClientLanguage>>().GetAll(with: i => i.Language))
            .Where(p => !p.Hidden)
            .Select(p => p.Language)
            .ToList();
        return Ok(langs);
    }
}

namespace Sencilla.Component.I18n;

[Route("api/v1/i18n/translate")]
public class TranslateController : ApiController
{
    private readonly ITranslateService _translateService;
    private readonly ITranslator _translator;

    public TranslateController(IResolver resolver, ITranslateService translateService, ITranslator translator) : base(resolver)
    {
        _translateService = translateService;
        _translator = translator;
    }

    [HttpGet("languages")] public async Task<IActionResult> GetLanguages() => Ok(await _translator.GetSupportedLanguages());

    [HttpGet] public async Task<IActionResult> Translate() => Ok(await _translator.TranslateText(["hello, world", "how are you?"], "en", "uk"));

    [HttpPost("language")]
    public async Task<IActionResult> Translate([FromBody] TranslateSettingsWe settingsWe)
    {
        var settings = new TranslateSettings
        {
            ProviderName = settingsWe.ProviderName,
            OnlyEmpty = settingsWe.OnlyEmpty
        };

        await _translateService.TranslateLanguage(settingsWe.LanguageIds.Single(), settings);

        return Ok();
    }

    [HttpPost("language/all")]
    public async Task<IActionResult> TranslateAll([FromBody] TranslateSettingsWe settingsWe)
    {
        var settings = new TranslateSettings
        {
            ProviderName = settingsWe.ProviderName,
            OnlyEmpty = settingsWe.OnlyEmpty
        };

        foreach (var id in settingsWe.LanguageIds)
            await _translateService.TranslateLanguage(id, settings);

        return Ok();
    }
}

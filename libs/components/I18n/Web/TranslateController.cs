namespace Sencilla.Component.I18n;

[Route("api/v1/i18n/translate")]
public class TranslateController(IServiceProvider resolver, ITranslateService translateService, ITranslator translator) : ApiController(resolver)
{
    [HttpGet("languages")] 
    public async Task<IActionResult> GetLanguages() => Ok(await translator.GetSupportedLanguages());

    [HttpGet] 
    public async Task<IActionResult> Translate() => Ok(await translator.TranslateText(["hello, world", "how are you?"], "en", "uk"));

    [HttpPost("language")]
    public async Task<IActionResult> Translate([FromBody] TranslateSettings settingsWe)
    {
        var settings = new TranslateSettings
        {
            ProviderName = settingsWe.ProviderName,
            OnlyEmpty = settingsWe.OnlyEmpty
        };

        await translateService.TranslateLanguage(settingsWe.LanguageIds.Single(), settings);

        return Ok();
    }

    [HttpPost("language/all")]
    public async Task<IActionResult> TranslateAll([FromBody] TranslateSettings settingsWe)
    {
        var settings = new TranslateSettings
        {
            ProviderName = settingsWe.ProviderName,
            OnlyEmpty = settingsWe.OnlyEmpty
        };

        foreach (var id in settingsWe.LanguageIds)
            await translateService.TranslateLanguage(id, settings);

        return Ok();
    }
}

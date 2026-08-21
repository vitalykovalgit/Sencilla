namespace Sencilla.Component.I18n;

[Route("api/v1/i18n/translate")]
public class TranslateController(IServiceProvider resolver, ITranslateService translateService, ITranslator translator) : ApiController(resolver)
{
    [HttpGet("languages")] 
    public async Task<IActionResult> GetLanguages() => Ok(await translator.GetSupportedLanguages());

    [HttpGet] 
    public async Task<IActionResult> Translate() => Ok(await translator.TranslateText(["hello, world", "how are you?"], "en", "uk"));

    [HttpPost("language")]
    public async Task<IActionResult> Translate([FromBody] TranslateSettings settings)
    {
        if (settings.LanguageIds is not { Length: > 0 })
            return BadRequest("No target language was given.");

        await translateService.TranslateLanguage(settings.LanguageIds.Single(), settings);

        return Ok();
    }

    [HttpPost("language/all")]
    public async Task<IActionResult> TranslateAll([FromBody] TranslateSettings settings)
    {
        if (settings.LanguageIds is not { Length: > 0 })
            return BadRequest("No target languages were given.");

        // Sequential on purpose: the providers bill and rate-limit per call, and a fan-out here
        // turns a routine "translate everything" into a burst that gets the key throttled.
        foreach (var id in settings.LanguageIds)
            await translateService.TranslateLanguage(id, settings);

        return Ok();
    }
}

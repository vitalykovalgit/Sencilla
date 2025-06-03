//namespace Sencilla.Component.I18n;

//[Route("api/v1/i18n/clientLanguage")]
//public class ClientLanguageController : CrudApiController<ClientLanguage>
//{
//    public ClientLanguageController(IResolver resolver) : base(resolver) { }
//    // TODO: move it to frontend using filter
//    [HttpGet]
//    public override async Task<IActionResult> GetAll(Filter<ClientLanguage> filter, CancellationToken token) =>
//        await AjaxAction((IReadRepository<ClientLanguage> repo) => repo.GetAll(filter, token, with => with.Language));
//}

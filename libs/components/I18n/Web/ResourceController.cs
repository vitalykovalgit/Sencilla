//namespace Sencilla.Component.I18n;

//[Route("api/v1/i18n/resource")]
//public class ResourceController : ApiController
//{
//    private readonly IReadRepository<ResourceView, string> _resourceReadRepo;
//    public ResourceController(IResolver resolver,IReadRepository<ResourceView, string> resourceReadRepo) : base(resolver)
//    {
//        _resourceReadRepo = resourceReadRepo;
//    }

//    [HttpGet]
//    public async Task<IActionResult> GetResources(Filter<ResourceView> filter) => 
//        Ok((await _resourceReadRepo.GetAll(filter)).Select(x => new ResourceViewWe(x)));
//}

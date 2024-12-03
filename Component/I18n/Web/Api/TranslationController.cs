namespace Sencilla.Component.I18n;

[Route("api/v1/i18n/translation")]
public class TranslationController : CrudApiController<Translation>
{
    public TranslationController(IResolver resolver) : base(resolver) { }

    public override async Task<IActionResult> UpdateMany([FromBody] IEnumerable<Translation> entities, CancellationToken token)
    {
        return await AjaxAction(async (IUpdateRepository<Translation> repo) =>
        {
            var map = entities.ToDictionary(k =>  k.Id, v => v.Value);
            var dbEntities = await repo.GetByIds(map.Keys);
            dbEntities.ForEach(x => x.Value = map[x.Id]);
            return await repo.Update(dbEntities);
        });
    }
}


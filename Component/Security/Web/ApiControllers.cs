
using Microsoft.AspNetCore.Mvc;
using Sencilla.Core;

namespace Sencilla.Component.Security
{
    [Route("api/v1/roles")]
    public class RolesController : CrudApiController<Area>
    {
        public RolesController(IResolver resolver) : base(resolver) { }
    }
}

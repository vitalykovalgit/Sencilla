
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Sencilla.Core.Repo;
using Sencilla.Web.Api;
using Sencilla.Component.Files.Entity;
using Sencilla.Core.Logging;
using Sencilla.Component.Files.Web.Entity;
using Microsoft.Extensions.Configuration;

namespace Sencilla.Component.Files.Web.Api
{
    [Route("api/files")]
    public class FilesController : ApiController
    {
        readonly IReadRepository<File, long> mReadFileRepo;

        public FilesController(
            //ILogger logger,
            IReadRepository<File, long> readFileRepo)
        {
            //Logger = logger;
            mReadFileRepo = readFileRepo;
        }

        [HttpGet, Route("{fileId}")]
        public async Task<IActionResult> Get(long fileId)
        {
            var file = mReadFileRepo.GetById(fileId);
            if (file == null)
                return Ok("null");

            var we = new FileWe()
            {
                Id = fileId,
                Name = "File.txt"
            };

            return Ok(file);
        }
    }
}

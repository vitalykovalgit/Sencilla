using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using Sencilla.Core.Repo;
using Sencilla.Core.Logging;
using Sencilla.Core.Injection;

using Sencilla.Web.Api;
using Sencilla.Component.Files.Entity;
using Sencilla.Component.Files.Web.Entity;

namespace Sencilla.Component.Files.Web.Api
{
    [Route("api/files")]
    public partial class FilesController : ApiController
    {
        readonly IReadRepository<File, ulong> mReadFileRepo;
        readonly IUpdateRepository<File, ulong> mUpdateFileRepo;
        readonly ICreateRepository<File, ulong> mCreateFileRepo;
        readonly IRemoveRepository<File, ulong> mRemoveFileRepo;
        readonly IDeleteRepository<File, ulong> mDeleteFileRepo;

        readonly IFileContentProvider mContentProvider;

        public FilesController(
            ILogger logger,
            IResolver resolver,
            IFileContentProvider contentProvider,
            IReadRepository<File, ulong> readFileRepo,
            IUpdateRepository<File, ulong> updateFileRepo,
            ICreateRepository<File, ulong> createFileRepo,
            IRemoveRepository<File, ulong> removeFileRepo,
            IDeleteRepository<File, ulong> deleteFileRepo) : base(logger, resolver)
        {
            mReadFileRepo = readFileRepo;
            mUpdateFileRepo = updateFileRepo;
            mCreateFileRepo = createFileRepo;
            mRemoveFileRepo = removeFileRepo;
            mDeleteFileRepo = deleteFileRepo;

            mContentProvider = contentProvider;
        }

        /// <summary>
        /// Retrive file info from db 
        /// </summary>
        [HttpGet, Route("{fileId}")]
        public async Task<IActionResult> Get(ulong fileId, CancellationToken token)
        {
            var file = await mReadFileRepo.GetByIdAsync(fileId, token);
            if (file == null)
                return NotFound();

            return Ok(new FileWe(file));
        }

        /// <summary>
        /// stream 
        /// </summary>
        [HttpGet, Route("{fileId}/stream")]
        public async Task<FileStreamResult> GetStream(ulong fileId, CancellationToken token)
        {
            var file = await mReadFileRepo.GetByIdAsync(fileId, token);
            if (file == null)
                return null;

            var stream = await mContentProvider.ReadFileAsync(file, token);
            return new FileStreamResult(stream, file.MimeType);
        }

        /// <summary>
        /// Create file 
        /// </summary>
        [HttpPost, Route("")]
        public async Task<IActionResult> Create(UploadFileWe model, CancellationToken token)
        {
            // Validate model 
            if (model?.FormFile == null)
                return BadRequest("Uploaded file is null");

            // Convert to domain model 
            var uploadedFile = model.FormFile.ToSencillaFile();

            // Save file to storage
            using (var stream = model.FormFile.OpenReadStream())
            {
                uploadedFile = await mContentProvider.WriteFileAsync(uploadedFile, stream, token);
            }

            // Create file in DB 
            var createdFile = await mCreateFileRepo.CreateAsync(uploadedFile, token);
            if (createdFile == null)
                return InternalServerError();

            // return created file 
            return Ok(new FileWe(createdFile));
        }

        [HttpPut, Route("")]
        public async Task<IActionResult> Update(FileWe file)
        {
            throw new NotImplementedException();
        }

        [HttpPost, Route("remove")]
        public async Task<IActionResult> Remove(ulong fileId, CancellationToken token)
        {
            var file = await mReadFileRepo.GetByIdAsync(fileId, token);
            if (file == null)
                return NotFound();

            // Remove file 
            // TODO: Think about content 
            await mRemoveFileRepo.RemoveAsync(fileId, token);

            return Ok(fileId);
        }

        [HttpDelete, Route("")]
        public async Task<IActionResult> Delete(ulong fileId, CancellationToken token)
        {
            var file = await mDeleteFileRepo.GetByIdAsync(fileId, token);
            if (file == null)
                return NotFound();

            // Remove content 
            await mContentProvider.DeleteFileAsync(file, token);

            // Remove from DB 
            await mDeleteFileRepo.DeleteAsync(token, fileId);

            return Ok(fileId);
        }
    }
}

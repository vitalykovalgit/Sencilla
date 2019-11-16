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
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Collections.Generic;
using System.Linq;

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
            //if (file == null)
            //    return NotFound();

            var stream = await mContentProvider.ReadFileAsync(file, token);
            //if (stream == null)
            //    return NotFound();

            // Response...
            //Response.Headers.Add("X-Content-Type-Options", "nosniff");
            //Response.Headers.Add("Accept-Ranges", "bytes");
            Response.Headers.Add("Content-Disposition", new ContentDisposition
            {
                FileName = file.Name,
                //false = prompt the user for downloading;  
                //true = browser to try to show the file inline
                Inline = true
            }.ToString());

            //return new FileStreamResult(stream, file.MimeType);
            return File(stream, file.MimeType, true);
        }

        /// <summary>
        /// Create file 
        /// </summary>
        [HttpPost, Route("")]
        public async Task<IActionResult> Create(UploadFileWe model, CancellationToken token)
        {
            try
            {
                // Validate model 
                if (model?.File == null)
                    return BadRequest("Uploaded file is null");

                // Convert to domain model 
                var uploadedFile = model.File.ToSencillaFile();

                // Create file in DB 
                var createdFile = await mCreateFileRepo.CreateAsync(uploadedFile, token);
                if (createdFile == null)
                    return InternalServerError();

                // Save file to storage
                using (var stream = model.File.OpenReadStream())
                {
                    uploadedFile = await mContentProvider.WriteFileAsync(createdFile, stream, token);
                }


                // return created file 
                return Ok(new FileWe(createdFile));
            }
            catch (Exception ex) 
            {
                throw;
            }
        }

        /// <summary>
        /// Create many files
        /// </summary>
        [HttpPost, Route("multiple")]
        public async Task<IActionResult> CreateMany(UploadFilesWe model, CancellationToken token)
        {
            try
            {
                if (model == null || model.Files.Count() == default)
                {
                    return BadRequest("Uploaded files are null");
                }

                Dictionary<int, File> uploadedFiles = model.Files.ToSencillaFiles();

                // Create files in DB 
                var createdFiles = mCreateFileRepo.Create(uploadedFiles.Select(uf => uf.Value));
                if (createdFiles == null)
                    return InternalServerError();

                foreach (IFormFile file in model.Files)
                {
                    using (System.IO.Stream stream = file.OpenReadStream())
                    {
                        File sencillaFile = uploadedFiles.First(uf => uf.Key == file.GetHashCode()).Value;
                        var uploadedFile1 = await mContentProvider.WriteFileAsync(sencillaFile, stream, token);
                    }
                }

                // convert to output result web entities
                List<FileWe> resultFiles = new List<FileWe>();
                createdFiles.ToList().ForEach(createdFile => resultFiles.Add(new FileWe(createdFile)));
                
                // return created files
                return Ok(resultFiles);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPut, Route("")]
        public Task<IActionResult> Update(FileWe file)
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

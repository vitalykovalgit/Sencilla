using System;
using Sencilla.Component.Files.Entity;

namespace Microsoft.AspNetCore.Http
{
    public static class IFormFileEx
    {
        public static File ToSencillaFile(this IFormFile formFile)
        {
            return formFile == null ? null : new File
            {
                Name = formFile.Name,
                Size = formFile.Length,
                //MimeType = ,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                DeletedDate = null
            };
        }
    }
}

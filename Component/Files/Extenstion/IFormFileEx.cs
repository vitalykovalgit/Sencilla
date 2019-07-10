﻿using System;
using System.Collections.Generic;
using Sencilla.Component.Files.Entity;

namespace Microsoft.AspNetCore.Http
{
    public static class IFormFileEx
    {
        private static Dictionary<string, string> ExtToMimeType = new Dictionary<string, string>  
        {  
            {".txt", "text/plain"},  
            {".pdf", "application/pdf"},  
            {".doc", "application/vnd.ms-word"},  
            {".docx", "application/vnd.ms-word"},  
            {".xls", "application/vnd.ms-excel"},  
            {".xlsx", "application/vnd.openxmlformats officedocument.spreadsheetml.sheet"},  
            {".png", "image/png"},  
            {".jpg", "image/jpeg"},  
            {".jpeg", "image/jpeg"},  
            {".gif", "image/gif"},  
            {".csv", "text/csv"}  
        };

        public static File ToSencillaFile(this IFormFile formFile)
        {
            return formFile == null ? null : new File
            {
                Size = formFile.Length,
                Name = formFile.FileName,
                Path = string.Empty,
                // TODO: Specify dictionary with mime types and use it 
                MimeType = "application/octet-stream",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                DeletedDate = null
            };
        }
    }
}

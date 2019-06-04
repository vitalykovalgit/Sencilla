using Microsoft.Extensions.Configuration;
using Sencilla.Infrastructure.SqlMapper.Contract;
using Sencilla.Infrastructure.SqlMapper.Impl;
using Sencilla.Component.Files.Entity;

namespace Sencilla.Component.Files.Impl
{
    public class FileDbContext : DbContext
    {
        public FileDbContext(IConfiguration config) : base(config)
        {
            Files = QuerySet<File, long>();
            FileContents = QuerySet<FileContent, long>();
        }

        public ISet<File, long> Files { get; }
        public ISet<FileContent, long> FileContents { get; }
    }

}

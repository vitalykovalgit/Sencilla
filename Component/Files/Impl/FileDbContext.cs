//using Microsoft.Extensions.Configuration;
//using Sencilla.Infrastructure.SqlMapper.Contract;
//using Sencilla.Infrastructure.SqlMapper.Impl;
//using Sencilla.Component.Files;

//namespace Sencilla.Component.Files.Impl
//{
//    public class FileDbContext : DbContext
//    {
//        public FileDbContext(IConfiguration config) : base(config)
//        {
//            Files = QuerySet<File, ulong>();
//            FileContents = QuerySet<FileContent, ulong>();
//        }

//        public ISet<File, ulong> Files { get; }
//        public ISet<FileContent, ulong> FileContents { get; }
//    }

//}

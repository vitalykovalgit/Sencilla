namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    /// <summary>
    /// Class that is mapped to TInclude SQL parameter type.
    /// </summary>
    public class Include
    {
        public string ParentTable { get; set; }
        public string ParentKey { get; set; }
        public string JoinedTable { get; set; }
        public string JoinedKey { get; set; }
    }
}

namespace Sencilla.Infrastructure.SqlMapper.Mapping
{
    public interface ITableMappingCache
    {
        ITableMapping<TEntity> GetTableMapping<TEntity>() where TEntity : class, new();
    }
}
